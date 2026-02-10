using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _repository;
    private readonly WebAPI.Services.AuditService _auditService;

    public UsersController(IUserRepository repository, WebAPI.Services.AuditService auditService)
    {
        _repository = repository;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return Ok(await _repository.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(User user)
    {
        await _repository.AddAsync(user);
        await _repository.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(WebAPI.Models.ChangePasswordRequest request)
    {
        var username = User.Identity?.Name;
        if (username == null) return Unauthorized();

        var user = await _repository.GetByUsernameAsync(username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
        {
            return BadRequest("Invalid current password.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        _repository.Update(user);
        await _repository.SaveChangesAsync();

        await _auditService.LogAsync("ChangePassword", "User", user.Id.ToString(), $"Username: {user.Username}", user.Id);

        return Ok(new { message = "Password changed successfully." });
    }
}
