using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ITodoTaskRepository _todoTaskRepository;

    public CalendarController(IAppointmentRepository appointmentRepository, ITodoTaskRepository todoTaskRepository)
    {
        _appointmentRepository = appointmentRepository;
        _todoTaskRepository = todoTaskRepository;
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents()
    {
        var appointments = await _appointmentRepository.GetAllAsync();
        var tasks = await _todoTaskRepository.GetAllAsync();

        var events = new List<object>();

        foreach (var appt in appointments)
        {
            events.Add(new
            {
                id = $"appt-{appt.Id}",
                title = appt.Description,
                start = appt.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = appt.StartTime.Add(appt.Duration).ToString("yyyy-MM-ddTHH:mm:ss"),
                backgroundColor = "#007bff",
                extendedProps = new { type = "appointment", dbId = appt.Id }
            });
        }

        foreach (var task in tasks)
        {
            if (task.Deadline.HasValue)
            {
                events.Add(new
                {
                    id = $"task-{task.Id}",
                    title = $"[TASK] {task.Title}",
                    start = task.Deadline.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                    allDay = true,
                    backgroundColor = "#28a745",
                    extendedProps = new { type = "task", dbId = task.Id }
                });
            }
        }

        return Ok(events);
    }

    [HttpPost("update-event")]
    public async Task<IActionResult> UpdateEvent([FromBody] UpdateEventRequest request)
    {
        if (request.Type == "appointment")
        {
            var appt = await _appointmentRepository.GetByIdAsync(request.DbId);
            if (appt == null) return NotFound();
            appt.StartTime = request.NewDate;
            _appointmentRepository.Update(appt);
            await _appointmentRepository.SaveChangesAsync();
        }
        else if (request.Type == "task")
        {
            var task = await _todoTaskRepository.GetByIdAsync(request.DbId);
            if (task == null) return NotFound();
            task.Deadline = request.NewDate;
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
}
