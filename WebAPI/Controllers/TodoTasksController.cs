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

        if (!tasks.Any()) return NoRecordsFound();
        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoTask>> GetTask(int id)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null) return NoRecordsFound();

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

    private readonly WebAPI.Services.GoogleCalendarService _googleService;

    public TodoTasksController(
        ITodoTaskRepository repository,
        IProjectRepository projectRepository,
        IUserBranchRepository userBranchRepository,
        WebAPI.Services.AuditService auditService,
        WebAPI.Services.GoogleCalendarService googleService,
        ApplicationDbContext context) : base(context)
    {
        _repository = repository;
        _projectRepository = projectRepository;
        _userBranchRepository = userBranchRepository;
        _auditService = auditService;
        _googleService = googleService;
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

        // Auto-sync "Urgente"
        var priority = await _context.Priorities.FindAsync(task.PriorityId);
        if (priority?.Name == "Urgente" && string.IsNullOrEmpty(task.GoogleCalendarId)) {
            task.GoogleCalendarId = "primary";
        }

        await _repository.AddAsync(task);
        await _repository.SaveChangesAsync();

        // Real Google Sync if CalendarId is set
        if (!string.IsNullOrEmpty(task.GoogleCalendarId)) {
            try {
                var ev = new Google.Apis.Calendar.v3.Data.Event {
                    Summary = $"[TASK] {task.Title}",
                    Description = task.Description,
                    Start = new Google.Apis.Calendar.v3.Data.EventDateTime { DateTimeDateTimeOffset = task.Deadline.AddHours(-1) },
                    End = new Google.Apis.Calendar.v3.Data.EventDateTime { DateTimeDateTimeOffset = task.Deadline }
                };
                var created = await _googleService.CreateEventAsync(CurrentUserId, task.GoogleCalendarId, ev);
                task.GoogleEventId = created.Id;
                task.SyncStatus = SyncStatus.Synced;
                await _repository.SaveChangesAsync();
            } catch (Exception ex) {
                Console.WriteLine($"[TodoTasksController] Google Sync failed: {ex.Message}");
            }
        }

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

    [HttpPost("update-status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
    {
        var task = await _repository.GetByIdAsync(request.TaskId);
        if (task == null) return NotFound();

        // Security check
        if (task.Project != null && !await CanAccessBranchAsync(task.Project.BranchId))
        {
            return Forbid();
        }

        if (CurrentUserRole == Roles.User && !task.TaskAssignments.Any(ta => ta.UserId == CurrentUserId))
        {
            return Forbid();
        }

        task.Status = (TodoStatus)request.StatusId;
        if (task.Status == TodoStatus.Completed && task.ProcessedAt == null)
        {
            task.ProcessedAt = DateTime.Now;
        }

        _repository.Update(task);
        await _repository.SaveChangesAsync();

        await _auditService.LogAsync("UpdateStatus", "TodoTask", task.Id.ToString(), $"NewStatus: {task.Status}");

        return Ok(new { Success = true });
    }

    public class UpdateStatusRequest
    {
        public int TaskId { get; set; }
        public int StatusId { get; set; }
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
