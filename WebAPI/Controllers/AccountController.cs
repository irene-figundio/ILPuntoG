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
    private readonly WebAPI.Services.GoogleCalendarService _googleService;

    public AccountController(IUserRepository userRepository, IConfiguration configuration, WebAPI.Services.AuditService auditService, WebAPI.Services.GoogleCalendarService googleService)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _auditService = auditService;
        _googleService = googleService;
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

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            var (email, tokens) = await _googleService.ExchangeCodeAsync(request.Code, request.RedirectUri);
            var user = await _userRepository.GetByUsernameAsync(email); // Using email as username

            if (user == null)
            {
                user = new User
                {
                    Name = email.Split('@')[0],
                    Email = email,
                    Username = email,
                    PasswordHash = "GOOGLE_SSO",
                    RoleId = 1
                };
                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();
            }

            // Persist Google Tokens
            await _googleService.PersistGoogleTokensAsync(user.Id, tokens, email);

            var token = GenerateJwtToken(user);
            await _auditService.LogAsync("GoogleLogin", "User", user.Id.ToString(), $"Email: {user.Email}", user.Id);

            return Ok(new { token, user = new { user.Id, user.Username, user.Name, user.Email } });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public class GoogleLoginRequest
    {
        public string Code { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var users = await _userRepository.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == request.Email);
        if (user == null)
        {
            return BadRequest("User with this email not found.");
        }

        await _auditService.LogAsync("ForgotPassword", "User", user.Id.ToString(), $"Email: {user.Email}", user.Id);

        return Ok(new { message = "Se l'indirizzo email è registrato, riceverai le istruzioni per reimpostare la password." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null) return BadRequest("Invalid request.");

        var secret = _configuration["Jwt:Key"] ?? "default_secret_key";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(user.Username + user.Email));
        var expectedToken = Convert.ToBase64String(hash);

        if (request.Token != expectedToken) return BadRequest("Invalid or expired token.");

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
