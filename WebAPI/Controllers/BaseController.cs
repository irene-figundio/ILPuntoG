using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WebAPI.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class BaseController : ControllerBase
{
    protected readonly ApplicationDbContext _context;

    protected BaseController(ApplicationDbContext context)
    {
        _context = context;
    }

    protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    protected string CurrentUserRole => User.FindFirstValue(ClaimTypes.Role) ?? Roles.User;
    protected bool IsSuperAdmin => CurrentUserRole == Roles.SuperAdmin;

    protected async Task<List<int>> GetUserBranchIdsAsync()
    {
        if (IsSuperAdmin)
        {
            return await _context.Branches.Select(b => b.Id).ToListAsync();
        }

        return await _context.UserBranches
            .Where(ub => ub.UserId == CurrentUserId)
            .Select(ub => ub.BranchId)
            .ToListAsync();
    }

    protected async Task<bool> CanAccessBranchAsync(int branchId)
    {
        if (IsSuperAdmin) return true;
        return await _context.UserBranches.AnyAsync(ub => ub.UserId == CurrentUserId && ub.BranchId == branchId);
    }
}
