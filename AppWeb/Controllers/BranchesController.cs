using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Threading.Tasks;

namespace AppWeb.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class BranchesController : Controller
{
    private readonly ApiService _apiService;

    public BranchesController(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IActionResult> Index()
    {
        var branches = await _apiService.GetBranchesAsync();
        return View(branches);
    }

    public async Task<IActionResult> Details(int id)
    {
        var branch = await _apiService.GetBranchAsync(id);
        if (branch == null) return NotFound();
        return View(branch);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Branch branch)
    {
        if (ModelState.IsValid)
        {
            await _apiService.CreateBranchAsync(branch);
            return RedirectToAction(nameof(Index));
        }
        return View(branch);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var branch = await _apiService.GetBranchAsync(id);
        if (branch == null) return NotFound();
        return View(branch);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Branch branch)
    {
        if (ModelState.IsValid)
        {
            await _apiService.UpdateBranchAsync(id, branch);
            return RedirectToAction(nameof(Index));
        }
        return View(branch);
    }

    public async Task<IActionResult> Delete(int id)
    {
        await _apiService.DeleteBranchAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
