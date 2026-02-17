using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AppWeb.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class CalendarController : BaseController
{
    public CalendarController(ApiService apiService) : base(apiService)
    {
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.GoogleStatus = await _apiService.GetGoogleCalendarStatusAsync();
        ViewBag.Projects = await _apiService.GetProjectsAsync(CurrentBranchId);
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents(DateTime start, DateTime end, int? branchId, int? projectId)
    {
        var events = await _apiService.GetCalendarEventsAsync(start: start, end: end, branchId: branchId ?? CurrentBranchId, projectId: projectId);
        return Json(events);
    }

    [HttpGet]
    public async Task<IActionResult> GetGoogleStatus()
    {
        var status = await _apiService.GetGoogleCalendarStatusAsync();
        return Json(status);
    }

    [HttpGet]
    public async Task<IActionResult> GetGoogleEvents(string calendarId, DateTime start, DateTime end)
    {
        var events = await _apiService.GetGoogleEventsAsync(calendarId, start, end);
        return Json(events);
    }

    [HttpGet]
    public async Task<IActionResult> GetGoogleCalendars()
    {
        var calendars = await _apiService.GetGoogleCalendarsAsync();
        return Json(calendars);
    }

    [HttpGet]
    public async Task<IActionResult> GetTimeline(DateTime start, DateTime end)
    {
        var timeline = await _apiService.GetTimelineAsync(start, end);
        return Json(timeline);
    }

    public async Task<IActionResult> ConnectGoogle(string redirectUri)
    {
        var callbackUrl = Url.Action("GoogleCallback", "Calendar", null, Request.Scheme) ?? "";
        var authUrl = await _apiService.GetGoogleAuthUrlAsync(callbackUrl);
        return Redirect(authUrl ?? "/Calendar");
    }

    public async Task<IActionResult> GoogleCallback(string code)
    {
        var callbackUrl = Url.Action("GoogleCallback", "Calendar", null, Request.Scheme) ?? "";
        await _apiService.HandleGoogleCallbackAsync(code, callbackUrl);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> AllocateTasks()
    {
        await _apiService.AllocateTasksAsync();
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> SyncShortenedTask([FromBody] SyncShortenedRequest request)
    {
        await _apiService.SyncShortenedTaskAsync(request.CalendarId, request.EventId, request.NewDurationHours);
        return Ok();
    }

    public class SyncShortenedRequest
    {
        public string CalendarId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public double NewDurationHours { get; set; }
    }
}
