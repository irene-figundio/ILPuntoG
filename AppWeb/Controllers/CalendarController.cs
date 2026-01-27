using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppWeb.Controllers;

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
    public async Task<IActionResult> UpdateEvent(string type, int dbId, DateTime newDate)
    {
        await _apiService.UpdateCalendarEventAsync(type, dbId, newDate);
        return Ok();
    }
}
