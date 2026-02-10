using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System.Threading.Tasks;

namespace AppWeb.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class ProjectsController : Controller
{
    private readonly ApiService _apiService;

    public ProjectsController(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IActionResult> Index(int? branchId)
    {
        var projects = await _apiService.GetProjectsAsync(branchId);
        ViewBag.BranchId = branchId;
        var branches = await _apiService.GetBranchesAsync();
        ViewBag.Branches = new SelectList(branches, "Id", "Name", branchId);
        return View(projects);
    }

    public async Task<IActionResult> Details(int id)
    {
        var project = await _apiService.GetProjectAsync(id);
        if (project == null) return NotFound();
        return View(project);
    }

    public async Task<IActionResult> Create(int? branchId)
    {
        var branches = await _apiService.GetBranchesAsync();
        ViewBag.Branches = new SelectList(branches, "Id", "Name", branchId);
        return View(new Project { BranchId = branchId ?? 0 });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Project project)
    {
        await _apiService.CreateProjectAsync(project);
        return RedirectToAction(nameof(Index), new { branchId = project.BranchId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var project = await _apiService.GetProjectAsync(id);
        if (project == null) return NotFound();
        var branches = await _apiService.GetBranchesAsync();
        ViewBag.Branches = new SelectList(branches, "Id", "Name", project.BranchId);
        return View(project);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Project project)
    {
        await _apiService.UpdateProjectAsync(id, project);
        return RedirectToAction(nameof(Index), new { branchId = project.BranchId });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var project = await _apiService.GetProjectAsync(id);
        if (project == null) return NotFound();
        await _apiService.DeleteProjectAsync(id);
        return RedirectToAction(nameof(Index), new { branchId = project.BranchId });
    }
}
