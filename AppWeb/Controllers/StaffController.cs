using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System.Threading.Tasks;

namespace AppWeb.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class StaffController : Controller
{
    private readonly ApiService _apiService;

    public StaffController(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IActionResult> Index(int? branchId)
    {
        var users = await _apiService.GetUsersAsync();
        var branches = await _apiService.GetBranchesAsync();

        ViewBag.Branches = new SelectList(branches, "Id", "Name", branchId);
        ViewBag.BranchId = branchId;

        if (branchId.HasValue)
        {
            users = users.Where(u => u.UserBranches.Any(ub => ub.BranchId == branchId.Value)).ToList();
        }

        return View(users);
    }
}
