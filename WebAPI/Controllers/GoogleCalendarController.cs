using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Services;
using Google.Apis.Calendar.v3.Data;
using Microsoft.EntityFrameworkCore;
using Repository;
using Models;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class GoogleCalendarController : ControllerBase
{
    private readonly GoogleCalendarService _googleService;
    private readonly ApplicationDbContext _context;

    public GoogleCalendarController(GoogleCalendarService googleService, ApplicationDbContext context)
    {
        _googleService = googleService;
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var cred = await _context.GoogleCredentials.AnyAsync(g => g.UserId == CurrentUserId);
        return Ok(new { IsConnected = cred });
    }

    [HttpGet("calendars")]
    public async Task<IActionResult> GetCalendars()
    {
        try {
            var calendars = await _googleService.GetUserCalendarsAsync(CurrentUserId);
            return Ok(calendars);
        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(string calendarId, DateTime start, DateTime end)
    {
        try {
            var events = await _googleService.GetEventsAsync(CurrentUserId, calendarId, start, end);
            return Ok(events.Select(e => new {
                id = e.Id,
                title = e.Summary,
                start = e.Start.DateTimeDateTimeOffset ?? (object)e.Start.Date,
                end = e.End.DateTimeDateTimeOffset ?? (object)e.End.Date,
                backgroundColor = "#2b8cee", // Default, will be overridden by client-side mapping if needed
                extendedProps = new {
                    description = e.Description,
                    location = e.Location,
                    calendarId = calendarId
                }
            }));
        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent(string calendarId, [FromBody] Event ev)
    {
        try {
            var newEvent = await _googleService.CreateEventAsync(CurrentUserId, calendarId, ev);
            return Ok(newEvent);
        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("events/{eventId}")]
    public async Task<IActionResult> UpdateEvent(string calendarId, string eventId, [FromBody] Event ev, [FromQuery] bool allSeries = false)
    {
        try {
            var updated = await _googleService.UpdateEventAsync(CurrentUserId, calendarId, eventId, ev, allSeries);
            return Ok(updated);
        } catch (Exception ex) {
            if (ex.Message.Contains("Conflict")) return Conflict(ex.Message);
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("events/{eventId}")]
    public async Task<IActionResult> DeleteEvent(string calendarId, string eventId)
    {
        try {
            await _googleService.DeleteEventAsync(CurrentUserId, calendarId, eventId);
            return NoContent();
        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("connect")]
    public async Task<IActionResult> Connect()
    {
        // In a real app, this would redirect to Google OAuth.
        // For this task, we'll simulate saving a credential.
        var existing = await _context.GoogleCredentials.FirstOrDefaultAsync(g => g.UserId == CurrentUserId);
        if (existing == null) {
            _context.GoogleCredentials.Add(new GoogleCredential {
                UserId = CurrentUserId,
                AccessToken = "simulated-access-token",
                RefreshToken = "simulated-refresh-token",
                Expiry = DateTime.UtcNow.AddHours(1)
            });
        } else {
            existing.AccessToken = "simulated-access-token";
            existing.Expiry = DateTime.UtcNow.AddHours(1);
        }
        await _context.SaveChangesAsync();
        return Ok(new { Success = true, Message = "Connesso con successo (simulato) a Google Calendar!" });
    }

    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        var cred = await _context.GoogleCredentials.FirstOrDefaultAsync(g => g.UserId == CurrentUserId);
        if (cred != null) {
            _context.GoogleCredentials.Remove(cred);
            await _context.SaveChangesAsync();
        }
        return Ok(new { Success = true });
    }
}
