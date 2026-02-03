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

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _apiService.GetCalendarEventsAsync();
        return Json(events);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateEvent(string type, int dbId, DateTime newDate, global::Models.TodoStatus? status)
    {
        await _apiService.UpdateCalendarEventAsync(type, dbId, newDate, status);
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

    [HttpPost]
    public async Task<IActionResult> SyncGoogle()
    {
        return Json(await _apiService.SyncGoogleCalendarAsync());
    }

    [HttpPost]
    public async Task<IActionResult> DisconnectGoogle()
    {
        return Json(await _apiService.DisconnectGoogleCalendarAsync());
    }
}
