using System.Diagnostics;
using AppWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace AppWeb.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppWeb.Services.ApiService _apiService;

        public HomeController(ILogger<HomeController> logger, AppWeb.Services.ApiService apiService)
        {
            _logger = logger;
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Stats = await _apiService.GetDashboardStatsAsync();
            ViewBag.KanbanTasks = await _apiService.GetKanbanTasksAsync();
            ViewBag.BranchesSummary = await _apiService.GetBranchesSummaryAsync();

            if (User.IsInRole(global::Models.Roles.SuperAdmin))
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
}
