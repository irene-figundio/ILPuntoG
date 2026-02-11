using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TodoTasksController : BaseController
{
    private readonly ITodoTaskRepository _repository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserBranchRepository _userBranchRepository;
    private readonly WebAPI.Services.AuditService _auditService;

    public TodoTasksController(
        ITodoTaskRepository repository,
        IProjectRepository projectRepository,
        IUserBranchRepository userBranchRepository,
        WebAPI.Services.AuditService auditService,
        ApplicationDbContext context) : base(context)
    {
        _repository = repository;
        _projectRepository = projectRepository;
        _userBranchRepository = userBranchRepository;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoTask>>> GetTasks(int? projectId)
    {
        var allowedBranchIds = await GetUserBranchIdsAsync();
        var tasks = await _repository.GetAllAsync();

        // Filter by branch
        tasks = tasks.Where(t => t.Project != null && allowedBranchIds.Contains(t.Project.BranchId));

        if (CurrentUserRole == Roles.User)
        {
            // Regular users only see tasks assigned to them
            tasks = tasks.Where(t => t.TaskAssignments.Any(ta => ta.UserId == CurrentUserId));
        }

        if (projectId.HasValue)
        {
            tasks = tasks.Where(t => t.ProjectId == projectId.Value);
        }

        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoTask>> GetTask(int id)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null) return NotFound();

        if (task.Project != null && !await CanAccessBranchAsync(task.Project.BranchId))
        {
            return Forbid();
        }

        if (CurrentUserRole == Roles.User && !task.TaskAssignments.Any(ta => ta.UserId == CurrentUserId))
        {
            return Forbid();
        }

        return Ok(task);
    }

    public class CreateTaskRequest
    {
        public TodoTask Task { get; set; } = new();
        public List<int>? SelectedUserIds { get; set; }
        public bool AssignToAllInBranch { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<TodoTask>> CreateTask(CreateTaskRequest request)
    {
        if (CurrentUserRole == Roles.User) return Forbid();

        var task = request.Task;
        var project = await _projectRepository.GetByIdAsync(task.ProjectId);
        if (project == null) return BadRequest("Project not found");

        if (!await CanAccessBranchAsync(project.BranchId))
        {
            return Forbid();
        }

        await _repository.AddAsync(task);
        await _repository.SaveChangesAsync();

        await _auditService.LogAsync("Create", "TodoTask", task.Id.ToString(), $"Title: {task.Title}");

        if (request.AssignToAllInBranch)
        {
            var userBranches = await _userBranchRepository.FindAsync(ub => ub.BranchId == project.BranchId);
            foreach (var ub in userBranches)
            {
                if (task.TaskAssignments.All(ta => ta.UserId != ub.UserId))
                {
                    task.TaskAssignments.Add(new TaskAssignment { TodoTaskId = task.Id, UserId = ub.UserId });
                }
            }
        }

        if (request.SelectedUserIds != null)
        {
            foreach (var userId in request.SelectedUserIds)
            {
                if (task.TaskAssignments.All(ta => ta.UserId != userId))
                {
                    task.TaskAssignments.Add(new TaskAssignment { TodoTaskId = task.Id, UserId = userId });
                }
            }
        }

        await _repository.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, TodoTask task)
    {
        if (id != task.Id) return BadRequest();

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        // Check access to branch
        if (existing.Project != null && !await CanAccessBranchAsync(existing.Project.BranchId))
        {
            return Forbid();
        }

        // If simple User, they can only update status if assigned
        if (CurrentUserRole == Roles.User)
        {
            if (!existing.TaskAssignments.Any(ta => ta.UserId == CurrentUserId))
            {
                return Forbid();
            }

            // Limit what a user can update (just status and maybe processedat)
            existing.Status = task.Status;
            if (task.Status == TodoStatus.Completed && existing.ProcessedAt == null)
            {
                existing.ProcessedAt = DateTime.Now;
            }
        }
        else
        {
            // Admin/SuperAdmin can update everything
            if (task.Status == TodoStatus.Completed && existing.ProcessedAt == null)
            {
                task.ProcessedAt = DateTime.Now;
            }

            // Re-fetch to ensure we don't accidentally update things we shouldn't if they tried to bypass
            _repository.Update(task);
        }

        await _repository.SaveChangesAsync();
        await _auditService.LogAsync("Update", "TodoTask", task.Id.ToString(), $"Status: {task.Status}");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        if (CurrentUserRole == Roles.User) return Forbid();

        var task = await _repository.GetByIdAsync(id);
        if (task == null) return NotFound();

        if (task.Project != null && !await CanAccessBranchAsync(task.Project.BranchId))
        {
            return Forbid();
        }

        _repository.Remove(task);
        await _repository.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "TodoTask", id.ToString(), $"Title: {task.Title}");

        return NoContent();
    }
}
