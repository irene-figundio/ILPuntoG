using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Security.Claims;

namespace AppWeb.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class WorkLogsController : Controller
{
    private readonly ApiService _apiService;

    public WorkLogsController(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var logs = await _apiService.GetWorkLogsAsync(userId);
        return View(logs);
    }

    [HttpPost]
    public async Task<IActionResult> Create(WorkLog log)
    {
        log.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        await _apiService.CreateWorkLogAsync(log);
        return RedirectToAction(nameof(Index));
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> Report(int? month, int? year)
    {
        month ??= DateTime.Now.Month;
        year ??= DateTime.Now.Year;
        ViewBag.Month = month;
        ViewBag.Year = year;
        var report = await _apiService.GetWorkLogReportAsync(month.Value, year.Value);
        return View(report);
    }
}
