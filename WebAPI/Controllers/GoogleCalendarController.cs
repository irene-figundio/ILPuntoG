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
public class GoogleCalendarController : BaseController
{
    private readonly GoogleCalendarService _googleService;

    public GoogleCalendarController(GoogleCalendarService googleService, ApplicationDbContext context) : base(context)
    {
        _googleService = googleService;
    }

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

    [HttpGet("connect")]
    public IActionResult Connect([FromQuery] string redirectUri)
    {
        var url = _googleService.GetAuthUrl(redirectUri);
        return Ok(new { AuthUrl = url });
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] GoogleCallbackRequest request)
    {
        try {
            await _googleService.ExchangeCodeForTokenAsync(CurrentUserId, request.Code, request.RedirectUri);
            return Ok(new { Success = true });
        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }

    public class GoogleCallbackRequest
    {
        public string Code { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
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

    [HttpPost("allocate")]
    public async Task<IActionResult> Allocate()
    {
        try {
            await _googleService.AllocateTasksAsync(CurrentUserId);
            return Ok(new { Success = true });
        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("capacity")]
    public async Task<IActionResult> GetCapacity()
    {
        try {
            var capacity = await _googleService.GetTeamCapacityAsync(CurrentUserId);
            return Ok(new { Capacity = capacity });
        } catch (Exception ex) {
            if (ex.Message.Contains("not connected")) return Ok(new { Capacity = 0.0 });
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("sync-shortened")]
    public async Task<IActionResult> SyncShortened([FromBody] SyncShortenedRequest request)
    {
        try {
            await _googleService.SyncTaskShortenedAsync(CurrentUserId, request.CalendarId, request.EventId, request.NewDurationHours);
            return Ok(new { Success = true });
        } catch (Exception ex) {
            return BadRequest(ex.Message);
        }
    }

    public class SyncShortenedRequest
    {
        public string CalendarId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public double NewDurationHours { get; set; }
    }
}
