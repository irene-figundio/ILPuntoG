using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System.Threading.Tasks;

namespace AppWeb.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class TodoTasksController : Controller
{
    private readonly ApiService _apiService;

    public TodoTasksController(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IActionResult> Index(int? projectId)
    {
        var tasks = await _apiService.GetTasksAsync(projectId);
        ViewBag.ProjectId = projectId;
        var projects = await _apiService.GetProjectsAsync();
        ViewBag.Projects = new SelectList(projects, "Id", "Name", projectId);
        return View(tasks);
    }

    public async Task<IActionResult> Details(int id)
    {
        var task = await _apiService.GetTaskAsync(id);
        if (task == null) return NotFound();
        return View(task);
    }

    public async Task<IActionResult> Create(int? projectId)
    {
        var projects = await _apiService.GetProjectsAsync();
        ViewBag.Projects = new SelectList(projects, "Id", "Name", projectId);
        ViewBag.Priorities = new SelectList(await _apiService.GetPrioritiesAsync(), "Id", "Name");
        ViewBag.TaskTypes = new SelectList(await _apiService.GetTaskTypesAsync(), "Id", "Name");
        ViewBag.Users = new SelectList(await _apiService.GetUsersAsync(), "Id", "Name");

        var clients = await _apiService.GetClientsAsync();
        ViewBag.Clients = new SelectList(clients, "Id", "Name");

        return View(new TodoTask { ProjectId = projectId ?? 0, Status = TodoStatus.Pending, Deadline = DateTime.Now.AddDays(1) });
    }

    [HttpPost]
    public async Task<IActionResult> Create(TodoTask task, List<int> SelectedUserIds, bool AssignToAll)
    {
        await _apiService.CreateTaskAsync(task, SelectedUserIds, AssignToAll);
        return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var task = await _apiService.GetTaskAsync(id);
        if (task == null) return NotFound();
        var projects = await _apiService.GetProjectsAsync();
        ViewBag.Projects = new SelectList(projects, "Id", "Name", task.ProjectId);
        return View(task);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, TodoTask task)
    {
        await _apiService.UpdateTaskAsync(id, task);
        return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var task = await _apiService.GetTaskAsync(id);
        if (task == null) return NotFound();
        await _apiService.DeleteTaskAsync(id);
        return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
    }
}
