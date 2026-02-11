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
public class DashboardController : BaseController
{
    private readonly ITodoTaskRepository _taskRepo;
    private readonly IBranchRepository _branchRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IClientRepository _clientRepo;
    private readonly IUserRepository _userRepo;

    public DashboardController(
        ITodoTaskRepository taskRepo,
        IBranchRepository branchRepo,
        IProjectRepository projectRepo,
        IClientRepository clientRepo,
        IUserRepository userRepo,
        ApplicationDbContext context) : base(context)
    {
        _taskRepo = taskRepo;
        _branchRepo = branchRepo;
        _projectRepo = projectRepo;
        _clientRepo = clientRepo;
        _userRepo = userRepo;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStats>> GetStats()
    {
        var allowedBranchIds = await GetUserBranchIdsAsync();
        var allTasks = await _taskRepo.GetAllAsync();

        var tasks = allTasks.Where(t => t.Project != null && allowedBranchIds.Contains(t.Project.BranchId));

        // Filter by user if regular user
        if (CurrentUserRole == Roles.User)
        {
            tasks = tasks.Where(t => t.TaskAssignments.Any(ta => ta.UserId == CurrentUserId));
        }

        var allProjects = await _projectRepo.GetAllAsync();
        var projects = allProjects.Where(p => allowedBranchIds.Contains(p.BranchId));

        var stats = new DashboardStats
        {
            TotalTasks = tasks.Count(),
            PendingTasks = tasks.Count(t => t.Status == TodoStatus.Pending),
            InProgressTasks = tasks.Count(t => t.Status == TodoStatus.InProgress),
            CompletedTasks = tasks.Count(t => t.Status == TodoStatus.Completed),
            TotalProjects = projects.Count(),
            UpcomingAppointments = _context.Appointments.Count(a => allowedBranchIds.Contains(a.BranchId) && a.StartTime >= DateTime.Now && a.StartTime <= DateTime.Now.AddDays(7))
        };

        return Ok(stats);
    }

    [HttpGet("kanban")]
    public async Task<IActionResult> GetKanbanTasks()
    {
        var allowedBranchIds = await GetUserBranchIdsAsync();
        var allTasks = await _taskRepo.GetAllAsync();

        var tasks = allTasks.Where(t => t.Project != null && allowedBranchIds.Contains(t.Project.BranchId));

        if (CurrentUserRole == Roles.User)
        {
            tasks = tasks.Where(t => t.TaskAssignments.Any(ta => ta.UserId == CurrentUserId));
        }

        return Ok(tasks.OrderBy(t => t.Deadline));
    }

    [HttpGet("branches-summary")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<IEnumerable<BranchSummary>>> GetBranchesSummary()
    {
        var branches = await _context.Branches
            .Include(b => b.Projects)
            .Include(b => b.ClientBranches)
            .Include(b => b.UserBranches)
                .ThenInclude(ub => ub.User)
            .ToListAsync();

        var summary = branches.Select(b => new BranchSummary
        {
            Id = b.Id,
            Name = b.Name,
            ClientCount = b.ClientBranches.Count,
            ProjectCount = b.Projects.Count,
            TaskCount = _context.TodoTasks.Count(t => t.Project != null && t.Project.BranchId == b.Id),
            CompletedTaskCount = _context.TodoTasks.Count(t => t.Project != null && t.Project.BranchId == b.Id && t.Status == TodoStatus.Completed),
            Team = b.UserBranches.Select(ub => ub.User?.Name ?? "").ToList()
        });

        return Ok(summary);
    }
}
