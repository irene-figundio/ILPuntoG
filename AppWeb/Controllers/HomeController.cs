using AppWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Diagnostics;
using AppWeb.Services;

namespace AppWeb.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger, ApiService apiService) : base(apiService)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Stats = await _apiService.GetDashboardStatsAsync();

        var kanbanTasks = await _apiService.GetKanbanTasksAsync();
        if (CurrentBranchId.HasValue)
        {
            kanbanTasks = kanbanTasks.Where(t => t.Project?.BranchId == CurrentBranchId.Value).ToList();
        }
        ViewBag.KanbanTasks = kanbanTasks;

        ViewBag.TeamCapacity = await _apiService.GetTeamCapacityAsync();

        if (User.IsInRole(Roles.SuperAdmin))
        {
            ViewBag.BranchesSummary = await _apiService.GetBranchesSummaryAsync();
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
