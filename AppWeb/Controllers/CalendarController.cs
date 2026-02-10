using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppWeb.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class CalendarController : Controller
{
    private readonly ApiService _apiService;

    public CalendarController(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Branches = await _apiService.GetBranchesAsync();
        ViewBag.Projects = await _apiService.GetProjectsAsync();
        ViewBag.Clients = await _apiService.GetClientsAsync();
        ViewBag.Users = await _apiService.GetUsersAsync();
        ViewBag.Priorities = await _apiService.GetPrioritiesAsync();
        ViewBag.TaskTypes = await _apiService.GetTaskTypesAsync();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents(int? userId, int? branchId, int? projectId, int? clientId)
    {
        var events = await _apiService.GetCalendarEventsAsync(userId, branchId, projectId, clientId);
        return Json(events);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateEvent(string type, int dbId, DateTime newDate, global::Models.TodoStatus? status, TimeSpan? duration)
    {
        await _apiService.UpdateCalendarEventAsync(type, dbId, newDate, status, duration);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetGoogleStatus()
    {
        return Json(await _apiService.GetGoogleCalendarStatusAsync());
    }

    [HttpPost]
    public async Task<IActionResult> ConnectGoogle()
    {
        return Json(await _apiService.ConnectGoogleCalendarAsync());
    }

    [HttpGet]
    public async Task<IActionResult> GetGoogleCalendars()
    {
        return Json(await _apiService.GetGoogleCalendarsAsync());
    }

    [HttpGet]
    public async Task<IActionResult> GetGoogleEvents(string calendarId, DateTime start, DateTime end)
    {
        return Json(await _apiService.GetGoogleEventsAsync(calendarId, start, end));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateGoogleEvent(string calendarId, string eventId, [FromBody] object ev)
    {
        var success = await _apiService.UpdateGoogleEventAsync(calendarId, eventId, ev);
        if (success) return Ok();
        return BadRequest();
    }

    [HttpPost]
    public async Task<IActionResult> DisconnectGoogle()
    {
        return Json(await _apiService.DisconnectGoogleCalendarAsync());
    }
}
