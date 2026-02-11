using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CalendarController : BaseController
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ITodoTaskRepository _todoTaskRepository;

    public CalendarController(IAppointmentRepository appointmentRepository, ITodoTaskRepository todoTaskRepository, ApplicationDbContext context) : base(context)
    {
        _appointmentRepository = appointmentRepository;
        _todoTaskRepository = todoTaskRepository;
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(int? userId, int? branchId, int? projectId, int? clientId)
    {
        var allowedBranchIds = await GetUserBranchIdsAsync();

        var appointments = await _appointmentRepository.GetAllAsync();
        var tasks = await _todoTaskRepository.GetAllAsync();

        // Security filter
        appointments = appointments.Where(a => allowedBranchIds.Contains(a.BranchId));
        tasks = tasks.Where(t => t.Project != null && allowedBranchIds.Contains(t.Project.BranchId));

        // Apply filters
        if (branchId.HasValue)
        {
            if (!allowedBranchIds.Contains(branchId.Value)) return Forbid();
            appointments = appointments.Where(a => a.BranchId == branchId.Value);
            tasks = tasks.Where(t => t.Project?.BranchId == branchId.Value);
        }
        if (projectId.HasValue)
        {
            appointments = appointments.Where(a => a.ProjectId == projectId.Value);
            tasks = tasks.Where(t => t.ProjectId == projectId.Value);
        }
        if (clientId.HasValue)
        {
            tasks = tasks.Where(t => t.ClientId == clientId.Value);
            appointments = appointments.Where(a => a.Project?.ClientId == clientId.Value);
        }
        if (userId.HasValue)
        {
            tasks = tasks.Where(t => t.TaskAssignments.Any(ta => ta.UserId == userId.Value));
        }

        var events = new List<object>();

        foreach (var appt in appointments)
        {
            var color = appt.Branch?.HexColor ?? "#007bff";
            var occurrenceDates = GetOccurrences(appt.StartTime, appt.IsRecurring, appt.Recurrence);

            foreach (var date in occurrenceDates)
            {
                events.Add(new
                {
                    id = $"appt-{appt.Id}-{date:yyyyMMdd}",
                    title = appt.Description,
                    start = date.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = date.Add(appt.Duration).ToString("yyyy-MM-ddTHH:mm:ss"),
                    backgroundColor = color,
                    borderColor = color,
                    extendedProps = new { type = "appointment", dbId = appt.Id, isRecurring = appt.IsRecurring }
                });
            }
        }

        foreach (var task in tasks)
        {
            var color = task.Project?.Branch?.HexColor ?? (task.Status == TodoStatus.Completed ? "#6c757d" : "#28a745");
            var occurrenceDates = GetOccurrences(task.Deadline, task.IsRecurring, task.Recurrence);

            foreach (var date in occurrenceDates)
            {
                events.Add(new
                {
                    id = $"task-{task.Id}-{date:yyyyMMdd}",
                    title = $"{(task.Status == TodoStatus.Completed ? "✅" : "🕒")} [TASK] {task.Title}",
                    start = date.ToString("yyyy-MM-ddTHH:mm:ss"),
                    allDay = true,
                    backgroundColor = color,
                    borderColor = color,
                    extendedProps = new { type = "task", dbId = task.Id, status = task.Status, isRecurring = task.IsRecurring }
                });
            }
        }

        return Ok(events);
    }

    private List<DateTime> GetOccurrences(DateTime start, bool isRecurring, TaskRecurrence recurrence)
    {
        var dates = new List<DateTime> { start };
        if (!isRecurring || recurrence == TaskRecurrence.None) return dates;

        var endPeriod = start.AddYears(1);
        var current = start;

        while (true)
        {
            switch (recurrence)
            {
                case TaskRecurrence.Daily: current = current.AddDays(1); break;
                case TaskRecurrence.Weekly: current = current.AddDays(7); break;
                case TaskRecurrence.Monthly: current = current.AddMonths(1); break;
                case TaskRecurrence.Yearly: current = current.AddYears(1); break;
                default: return dates;
            }

            if (current > endPeriod) break;
            dates.Add(current);
        }

        return dates;
    }

    [HttpPost("update-event")]
    public async Task<IActionResult> UpdateEvent([FromBody] UpdateEventRequest request)
    {
        if (request.Type == "appointment")
        {
            var appt = await _appointmentRepository.GetByIdAsync(request.DbId);
            if (appt == null) return NotFound();

            if (!await CanAccessBranchAsync(appt.BranchId)) return Forbid();

            appt.StartTime = request.NewDate;
            if (request.Duration.HasValue)
            {
                appt.Duration = request.Duration.Value;
            }
            _appointmentRepository.Update(appt);
            await _appointmentRepository.SaveChangesAsync();
        }
        else if (request.Type == "task")
        {
            var task = await _todoTaskRepository.GetByIdAsync(request.DbId);
            if (task == null) return NotFound();

            if (task.Project != null && !await CanAccessBranchAsync(task.Project.BranchId)) return Forbid();

            task.Deadline = request.NewDate;
            if (request.Status.HasValue)
            {
                task.Status = request.Status.Value;
                if (task.Status == TodoStatus.Completed && task.ProcessedAt == null)
                    task.ProcessedAt = DateTime.Now;
            }
            _todoTaskRepository.Update(task);
            await _todoTaskRepository.SaveChangesAsync();
        }
        else
        {
            return BadRequest("Invalid type");
        }

        return Ok();
    }
}

public class UpdateEventRequest
{
    public string Type { get; set; } = string.Empty;
    public int DbId { get; set; }
    public DateTime NewDate { get; set; }
    public TodoStatus? Status { get; set; }
    public TimeSpan? Duration { get; set; }
}
