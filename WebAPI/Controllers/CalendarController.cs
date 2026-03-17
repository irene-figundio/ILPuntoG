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
    private readonly WebAPI.Services.GoogleCalendarService _googleService;

    public CalendarController(
        IAppointmentRepository appointmentRepository,
        ITodoTaskRepository todoTaskRepository,
        WebAPI.Services.GoogleCalendarService googleService,
        ApplicationDbContext context) : base(context)
    {
        _appointmentRepository = appointmentRepository;
        _todoTaskRepository = todoTaskRepository;
        _googleService = googleService;
    }

    [HttpGet("timeline")]
    public async Task<IActionResult> GetTimeline(DateTime start, DateTime end)
    {
        var googleEvents = await _googleService.GetEventsAsync(CurrentUserId, "all", start, end);
        var dbTasks = await _todoTaskRepository.GetAllAsync();

        var allowedIds = await GetUserBranchIdsAsync();
        dbTasks = dbTasks.Where(t => t.Project != null && allowedIds.Contains(t.Project.BranchId));

        if (CurrentUserRole == Roles.User) {
            dbTasks = dbTasks.Where(t => t.TaskAssignments.Any(ta => ta.UserId == CurrentUserId));
        }

        var timeline = new List<object>();

        foreach (var e in googleEvents) {
            timeline.Add(new {
                source = "google",
                title = e.Summary,
                start = e.Start.DateTimeDateTimeOffset ?? (object)e.Start.Date,
                location = e.Location,
                isFocus = e.Summary.Contains("[Focus]")
            });
        }

        foreach (var t in dbTasks.Where(t => t.Deadline >= start && t.Deadline <= end)) {
            timeline.Add(new {
                source = "db",
                title = $"[TASK] {t.Title}",
                start = t.Deadline,
                location = t.Project?.Name,
                isFocus = false
            });
        }

        if (!timeline.Any()) return NoRecordsFound();
        return Ok(timeline.OrderBy(x => GetStartTime(x)));
    }

    private DateTime GetStartTime(object x) {
        var p = x.GetType().GetProperty("start");
        var val = p?.GetValue(x);
        if (val is DateTime dt) return dt;
        if (val is DateTimeOffset dto) return dto.DateTime;
        if (val is string s && DateTime.TryParse(s, out var dt2)) return dt2;
        return DateTime.MinValue;
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(DateTime? start, DateTime? end, int? userId, int? branchId, int? projectId, int? clientId)
    {
        var allowedBranchIds = await GetUserBranchIdsAsync();

        var appointments = await _appointmentRepository.GetAllAsync();
        var tasks = await _todoTaskRepository.GetAllAsync();

        // Date range filter
        if (start.HasValue) {
            appointments = appointments.Where(a => a.StartTime >= start.Value || a.IsRecurring);
            tasks = tasks.Where(t => t.Deadline >= start.Value || t.IsRecurring);
        }
        if (end.HasValue) {
            appointments = appointments.Where(a => a.StartTime <= end.Value || a.IsRecurring);
            tasks = tasks.Where(t => t.Deadline <= end.Value || t.IsRecurring);
        }

        // Security filter
        appointments = appointments.Where(a => allowedBranchIds.Contains(a.BranchId));
        tasks = tasks.Where(t => t.Project != null && allowedBranchIds.Contains(t.Project.BranchId));

        // Apply additional filters
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
                DateTime endDate;
                try { endDate = date.Add(appt.Duration); } catch { endDate = DateTime.MaxValue; }

                events.Add(new
                {
                    id = $"appt-{appt.Id}-{date:yyyyMMdd}",
                    title = appt.Description,
                    start = date.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = endDate.ToString("yyyy-MM-ddTHH:mm:ss"),
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

        if (!events.Any()) return NoRecordsFound();
        return Ok(events);
    }

    private List<DateTime> GetOccurrences(DateTime start, bool isRecurring, TaskRecurrence recurrence)
    {
        var dates = new List<DateTime> { start };
        if (!isRecurring || recurrence == TaskRecurrence.None) return dates;

        DateTime endPeriod;
        try { endPeriod = start.AddYears(1); } catch { endPeriod = DateTime.MaxValue; }

        var current = start;

        while (true)
        {
            try
            {
                switch (recurrence)
                {
                    case TaskRecurrence.Daily: current = current.AddDays(1); break;
                    case TaskRecurrence.Weekly: current = current.AddDays(7); break;
                    case TaskRecurrence.Monthly: current = current.AddMonths(1); break;
                    case TaskRecurrence.Yearly: current = current.AddYears(1); break;
                    default: return dates;
                }
            }
            catch
            {
                break; // If Add* throws, stop recurrence
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
