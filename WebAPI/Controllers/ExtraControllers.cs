using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskTypesController : BaseController
{
    private readonly ITaskTypeRepository _repository;
    public TaskTypesController(ITaskTypeRepository repository, ApplicationDbContext context) : base(context) { _repository = repository; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskType>>> Get()
    {
        var items = await _repository.GetAllAsync();
        if (!items.Any()) return NoRecordsFound();
        return Ok(items);
    }
}

[ApiController]
[Route("api/[controller]")]
public class PrioritiesController : BaseController
{
    private readonly IPriorityRepository _repository;
    public PrioritiesController(IPriorityRepository repository, ApplicationDbContext context) : base(context) { _repository = repository; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Priority>>> Get()
    {
        var items = await _repository.GetAllAsync();
        if (!items.Any()) return NoRecordsFound();
        return Ok(items);
    }
}

[ApiController]
[Route("api/[controller]")]
public class WorkLogsController : BaseController
{
    private readonly IWorkLogRepository _repository;
    private readonly WebAPI.Services.AuditService _auditService;
    public WorkLogsController(IWorkLogRepository repository, WebAPI.Services.AuditService auditService, ApplicationDbContext context) : base(context)
    {
        _repository = repository;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkLog>>> Get(int? userId, int? month, int? year)
    {
        var logs = await _repository.GetAllAsync();
        if (userId.HasValue) logs = logs.Where(l => l.UserId == userId.Value);
        if (month.HasValue) logs = logs.Where(l => l.Date.Month == month.Value);
        if (year.HasValue) logs = logs.Where(l => l.Date.Year == year.Value);

        var results = logs.OrderByDescending(l => l.Date);
        if (!results.Any()) return NoRecordsFound();
        return Ok(results);
    }

    [HttpGet("report")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<IEnumerable<WorkLogReportItem>>> GetReport(int month, int year)
    {
        var logs = await _repository.GetAllAsync();
        var filtered = logs.Where(l => l.Date.Month == month && l.Date.Year == year).ToList();

        var report = filtered.GroupBy(l => l.UserId)
            .Select(g => new WorkLogReportItem {
                UserName = g.First().User?.Name ?? "Utente Ignoto",
                TotalHours = (double)g.Sum(l => l.Hours),
                TotalDays = g.Select(l => l.Date.Date).Distinct().Count(),
                Ferie = (double)g.Where(l => l.Type == WorkLogType.Holiday).Sum(l => l.Hours),
                Permessi = (double)g.Where(l => l.Type == WorkLogType.Permit).Sum(l => l.Hours),
                Malattia = (double)g.Where(l => l.Type == WorkLogType.Sickness).Sum(l => l.Hours)
            });

        return Ok(report);
    }

    [HttpPost]
    public async Task<ActionResult<WorkLog>> Post(WorkLog log)
    {
        await _repository.AddAsync(log);
        await _repository.SaveChangesAsync();
        await _auditService.LogAsync("Create", "WorkLog", log.Id.ToString(), $"Hours: {log.Hours}, Type: {log.Type}");
        return Ok(log);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, WorkLog log)
    {
        if (id != log.Id) return BadRequest();
        _repository.Update(log);
        await _repository.SaveChangesAsync();
        await _auditService.LogAsync("Update", "WorkLog", log.Id.ToString(), $"Hours: {log.Hours}, Type: {log.Type}");
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var log = await _repository.GetByIdAsync(id);
        if (log == null) return NoRecordsFound();
        _repository.Remove(log);
        await _repository.SaveChangesAsync();
        await _auditService.LogAsync("Delete", "WorkLog", id.ToString(), $"Type: {log.Type}");
        return NoContent();
    }
}
