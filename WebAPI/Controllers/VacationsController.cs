using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Security.Claims;
using WebAPI.Services;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class VacationsController : BaseController
{
    private readonly GoogleCalendarService _googleService;

    public VacationsController(ApplicationDbContext context, GoogleCalendarService googleService) : base(context)
    {
        _googleService = googleService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Vacation>>> GetVacations()
    {
        var vacations = await _context.Vacations
            .Include(v => v.User)
            .OrderByDescending(v => v.StartDate)
            .ToListAsync();

        if (!vacations.Any()) return NoRecordsFound();
        return Ok(vacations);
    }

    [HttpPost]
    public async Task<ActionResult<Vacation>> AddVacation(Vacation vacation)
    {
        vacation.UserId = CurrentUserId;
        _context.Vacations.Add(vacation);
        await _context.SaveChangesAsync();

        // Sync with Google Calendar if connected
        try
        {
            var user = await _context.Users.FindAsync(CurrentUserId);
            var title = $"Ferie: {user?.Name ?? "Utente"}";
            var googleEvent = await _googleService.CreateAllDayEventAsync(CurrentUserId, "primary", title, vacation.StartDate, vacation.EndDate.AddDays(1));
            vacation.GoogleEventId = googleEvent.Id;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VacationsController] Google Sync failed: {ex.Message}");
        }

        return Ok(vacation);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVacation(int id)
    {
        var vacation = await _context.Vacations.FindAsync(id);
        if (vacation == null) return NotFound();
        if (vacation.UserId != CurrentUserId && !IsSuperAdmin) return Forbid();

        if (!string.IsNullOrEmpty(vacation.GoogleEventId))
        {
            try
            {
                await _googleService.DeleteEventAsync(CurrentUserId, "primary", vacation.GoogleEventId);
            }
            catch {}
        }

        _context.Vacations.Remove(vacation);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
