using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Models;
using Repository;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAPI.Models;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly WebAPI.Services.AuditService _auditService;

    public AccountController(IUserRepository userRepository, IConfiguration configuration, WebAPI.Services.AuditService auditService)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _auditService = auditService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (await _userRepository.GetByUsernameAsync(request.Username) != null)
        {
            return BadRequest("Username already exists.");
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = 1 // Default to 'User'
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        await _auditService.LogAsync("Register", "User", user.Id.ToString(), $"Username: {user.Username}", user.Id);

        return Ok(new { message = "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Invalid username or password.");
        }

        var token = GenerateJwtToken(user);

        await _auditService.LogAsync("Login", "User", user.Id.ToString(), $"Username: {user.Username}", user.Id);

        return Ok(new { token, user = new { user.Id, user.Username, user.Name, user.Email } });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var users = await _userRepository.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == request.Email);
        if (user == null)
        {
            // Don't reveal if user exists or not for security, but for this task we'll be helpful
            return BadRequest("User with this email not found.");
        }

        // In a real app, generate a unique token and send an email
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.Username));

        await _auditService.LogAsync("ForgotPassword", "User", user.Id.ToString(), $"Email: {user.Email}", user.Id);

        return Ok(new { message = "Password reset link has been generated (simulated)", token, username = user.Username });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null) return BadRequest("Invalid request.");

        // Simple validation of simulated token
        var expectedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.Username));
        if (request.Token != expectedToken) return BadRequest("Invalid token.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        await _auditService.LogAsync("ResetPassword", "User", user.Id.ToString(), $"Username: {user.Username}", user.Id);

        return Ok(new { message = "Password has been reset successfully." });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secret = jwtSettings["Key"];
        if (string.IsNullOrEmpty(secret))
        {
            throw new InvalidOperationException("JWT Key is not configured in appsettings.json");
        }
        var key = Encoding.ASCII.GetBytes(secret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "User")
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
