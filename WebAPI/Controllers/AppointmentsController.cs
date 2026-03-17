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
public class AppointmentsController : BaseController
{
    private readonly IAppointmentRepository _repository;
    private readonly WebAPI.Services.AuditService _auditService;

    public AppointmentsController(IAppointmentRepository repository, WebAPI.Services.AuditService auditService, ApplicationDbContext context) : base(context)
    {
        _repository = repository;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments(int? branchId)
    {
        var allowedIds = await GetUserBranchIdsAsync();

        IEnumerable<Appointment> appts;

        if (branchId.HasValue)
        {
            if (!allowedIds.Contains(branchId.Value)) return Forbid();
            appts = await _repository.GetAppointmentsByBranchAsync(branchId.Value);
        }
        else
        {
            var all = await _repository.GetAllAsync();
            appts = all.Where(a => allowedIds.Contains(a.BranchId));
        }

        if (!appts.Any()) return NoRecordsFound();
        return Ok(appts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Appointment>> GetAppointment(int id)
    {
        var appointment = await _repository.GetByIdAsync(id);
        if (appointment == null) return NoRecordsFound();

        if (!await CanAccessBranchAsync(appointment.BranchId)) return Forbid();

        return Ok(appointment);
    }

    [HttpPost]
    public async Task<ActionResult<Appointment>> CreateAppointment(Appointment appointment)
    {
        if (!await CanAccessBranchAsync(appointment.BranchId)) return Forbid();

        await _repository.AddAsync(appointment);
        await _repository.SaveChangesAsync();
        await _auditService.LogAsync("Create", "Appointment", appointment.Id.ToString(), appointment.Description);
        return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAppointment(int id, Appointment appointment)
    {
        if (id != appointment.Id) return BadRequest();

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        if (!await CanAccessBranchAsync(existing.BranchId)) return Forbid();
        if (existing.BranchId != appointment.BranchId && !await CanAccessBranchAsync(appointment.BranchId)) return Forbid();

        _context.Entry(existing).State = EntityState.Detached;
        _repository.Update(appointment);
        await _repository.SaveChangesAsync();
        await _auditService.LogAsync("Update", "Appointment", appointment.Id.ToString(), appointment.Description);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var appointment = await _repository.GetByIdAsync(id);
        if (appointment == null) return NotFound();

        if (!await CanAccessBranchAsync(appointment.BranchId)) return Forbid();

        _repository.Remove(appointment);
        await _repository.SaveChangesAsync();
        await _auditService.LogAsync("Delete", "Appointment", id.ToString(), appointment.Description);
        return NoContent();
    }
}
