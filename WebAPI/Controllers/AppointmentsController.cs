using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentRepository _repository;
    private readonly WebAPI.Services.AuditService _auditService;

    public AppointmentsController(IAppointmentRepository repository, WebAPI.Services.AuditService auditService)
    {
        _repository = repository;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments(int? branchId)
    {
        if (branchId.HasValue)
        {
            return Ok(await _repository.GetAppointmentsByBranchAsync(branchId.Value));
        }
        return Ok(await _repository.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Appointment>> GetAppointment(int id)
    {
        var appointment = await _repository.GetByIdAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }
        return Ok(appointment);
    }

    [HttpPost]
    public async Task<ActionResult<Appointment>> CreateAppointment(Appointment appointment)
    {
        await _repository.AddAsync(appointment);
        await _repository.SaveChangesAsync();
        await _auditService.LogAsync("Create", "Appointment", appointment.Id.ToString(), appointment.Description);
        return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var appointment = await _repository.GetByIdAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }
        _repository.Remove(appointment);
        await _repository.SaveChangesAsync();
        await _auditService.LogAsync("Delete", "Appointment", appointment.Id.ToString(), appointment.Description);
        return NoContent();
    }
}
