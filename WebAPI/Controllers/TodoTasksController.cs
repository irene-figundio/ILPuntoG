using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TodoTasksController : ControllerBase
{
    private readonly ITodoTaskRepository _repository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserBranchRepository _userBranchRepository;
    private readonly WebAPI.Services.AuditService _auditService;

    public TodoTasksController(ITodoTaskRepository repository, IProjectRepository projectRepository, IUserBranchRepository userBranchRepository, WebAPI.Services.AuditService auditService)
    {
        _repository = repository;
        _projectRepository = projectRepository;
        _userBranchRepository = userBranchRepository;
        _auditService = auditService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private string CurrentUserRole => User.FindFirstValue(ClaimTypes.Role) ?? Roles.User;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoTask>>> GetTasks(int? projectId)
    {
        var tasks = await _repository.GetAllAsync();

        if (CurrentUserRole == Roles.User)
        {
            // User can only see their own tasks
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
        if (task == null)
        {
            return NotFound();
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
        await _repository.AddAsync(task);
        await _repository.SaveChangesAsync();

        await _auditService.LogAsync("Create", "TodoTask", task.Id.ToString(), $"Title: {task.Title}");

        if (request.AssignToAllInBranch)
        {
            var project = await _projectRepository.GetByIdAsync(task.ProjectId);
            if (project != null)
            {
                var userBranches = await _userBranchRepository.FindAsync(ub => ub.BranchId == project.BranchId);
                foreach (var ub in userBranches)
                {
                    if (task.TaskAssignments.All(ta => ta.UserId != ub.UserId))
                    {
                        task.TaskAssignments.Add(new TaskAssignment { TodoTaskId = task.Id, UserId = ub.UserId });
                    }
                }
                await _repository.SaveChangesAsync();
            }
        }

        if (request.SelectedUserIds != null)
        {
            foreach (var userId in request.SelectedUserIds)
            {
                task.TaskAssignments.Add(new TaskAssignment { TodoTaskId = task.Id, UserId = userId });
            }
            await _repository.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, TodoTask task)
    {
        if (id != task.Id) return BadRequest();
        if (CurrentUserRole == Roles.User) return Forbid();

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        if (CurrentUserRole == Roles.Admin)
        {
            // Admin can only update their own tasks
            if (!existing.TaskAssignments.Any(ta => ta.UserId == CurrentUserId))
            {
                return Forbid();
            }
        }

        if (task.Status == TodoStatus.Completed && task.ProcessedAt == null)
        {
            task.ProcessedAt = DateTime.Now;
        }

        _repository.Update(task);
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

        if (CurrentUserRole == Roles.Admin)
        {
            if (!task.TaskAssignments.Any(ta => ta.UserId == CurrentUserId))
            {
                return Forbid();
            }
        }

        _repository.Remove(task);
        await _repository.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "TodoTask", task.Id.ToString(), $"Title: {task.Title}");

        return NoContent();
    }
}
