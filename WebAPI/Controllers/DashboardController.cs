using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ITodoTaskRepository _taskRepo;
    private readonly IBranchRepository _branchRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IClientRepository _clientRepo;
    private readonly IUserRepository _userRepo;
    private readonly ApplicationDbContext _context;

    public DashboardController(
        ITodoTaskRepository taskRepo,
        IBranchRepository branchRepo,
        IProjectRepository projectRepo,
        IClientRepository clientRepo,
        IUserRepository userRepo,
        ApplicationDbContext context)
    {
        _taskRepo = taskRepo;
        _branchRepo = branchRepo;
        _projectRepo = projectRepo;
        _clientRepo = clientRepo;
        _userRepo = userRepo;
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private string CurrentUserRole => User.FindFirstValue(ClaimTypes.Role) ?? Roles.User;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var tasks = await _taskRepo.GetAllAsync();

        // Filter by user if not superadmin
        if (CurrentUserRole != Roles.SuperAdmin)
        {
            tasks = tasks.Where(t => t.TaskAssignments.Any(ta => ta.UserId == CurrentUserId));
        }

        var stats = new
        {
            TotalTasks = tasks.Count(),
            PendingTasks = tasks.Count(t => t.Status == TodoStatus.Pending),
            InProgressTasks = tasks.Count(t => t.Status == TodoStatus.InProgress),
            CompletedTasks = tasks.Count(t => t.Status == TodoStatus.Completed),
            TotalProjects = (await _projectRepo.GetAllAsync()).Count(), // This could be filtered too
            UpcomingAppointments = _context.Appointments.Count(a => a.StartTime >= DateTime.Now && a.StartTime <= DateTime.Now.AddDays(7))
        };

        return Ok(stats);
    }

    [HttpGet("kanban")]
    public async Task<IActionResult> GetKanbanTasks()
    {
        var tasks = await _taskRepo.GetAllAsync();

        if (CurrentUserRole != Roles.SuperAdmin)
        {
            tasks = tasks.Where(t => t.TaskAssignments.Any(ta => ta.UserId == CurrentUserId));
        }

        return Ok(tasks.OrderBy(t => t.Deadline));
    }

    [HttpGet("branches-summary")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> GetBranchesSummary()
    {
        var branches = await _context.Branches
            .Include(b => b.Projects)
            .Include(b => b.ClientBranches)
            .Include(b => b.UserBranches)
                .ThenInclude(ub => ub.User)
            .ToListAsync();

        var summary = branches.Select(b => new
        {
            b.Id,
            b.Name,
            ClientCount = b.ClientBranches.Count,
            ProjectCount = b.Projects.Count,
            TaskCount = _context.TodoTasks.Count(t => t.Project != null && t.Project.BranchId == b.Id),
            Team = b.UserBranches.Select(ub => ub.User?.Name).ToList()
        });

        return Ok(summary);
    }
}
