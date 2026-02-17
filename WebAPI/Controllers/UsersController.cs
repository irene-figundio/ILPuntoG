using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : BaseController
{
    private readonly IUserRepository _repository;
    private readonly WebAPI.Services.AuditService _auditService;

    public UsersController(IUserRepository repository, WebAPI.Services.AuditService auditService, ApplicationDbContext context) : base(context)
    {
        _repository = repository;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        // Users can see other users if they share a branch or if they are admin
        if (IsSuperAdmin) return Ok(await _repository.GetAllAsync());

        var branchIds = await GetUserBranchIdsAsync();
        var users = await _context.UserBranches
            .Where(ub => branchIds.Contains(ub.BranchId))
            .Select(ub => ub.User)
            .Distinct()
            .ToListAsync();

        if (!users.Any()) return NoRecordsFound();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        if (id == 0) return NoRecordsFound();
        if (!IsSuperAdmin && id != CurrentUserId)
        {
            // Check if they share a branch
            var myBranches = await GetUserBranchIdsAsync();
            var targetBranches = await _context.UserBranches.Where(ub => ub.UserId == id).Select(ub => ub.BranchId).ToListAsync();
            if (!myBranches.Intersect(targetBranches).Any())
            {
                return Forbid();
            }
        }

        var user = await _repository.GetByIdAsync(id);
        if (user == null) return NoRecordsFound();
        return Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<User>> CreateUser(User user)
    {
        await _repository.AddAsync(user);
        await _repository.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, User user)
    {
        if (id != user.Id) return BadRequest();
        if (!IsSuperAdmin && id != CurrentUserId) return Forbid();

        _repository.Update(user);
        await _repository.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null) return NoRecordsFound();
        _repository.Remove(user);
        await _repository.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(WebAPI.Models.ChangePasswordRequest request)
    {
        var user = await _repository.GetByIdAsync(CurrentUserId);
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
