using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System;
using System.Threading.Tasks;

namespace AppWeb.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class AppointmentsController : Controller
{
    private readonly ApiService _apiService;

    public AppointmentsController(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IActionResult> Index(int? branchId)
    {
        var appointments = await _apiService.GetAppointmentsAsync(branchId);
        var branches = await _apiService.GetBranchesAsync();
        ViewBag.Branches = new SelectList(branches, "Id", "Name", branchId);
        ViewBag.BranchId = branchId;
        return View(appointments);
    }

    public async Task<IActionResult> Create(int? branchId)
    {
        var branches = await _apiService.GetBranchesAsync();
        var projects = await _apiService.GetProjectsAsync(branchId);
        ViewBag.Branches = new SelectList(branches, "Id", "Name", branchId);
        ViewBag.Projects = new SelectList(projects, "Id", "Name");
        return View(new Appointment { BranchId = branchId ?? 0, StartTime = DateTime.Now });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Appointment appointment)
    {
        await _apiService.CreateAppointmentAsync(appointment);
        return RedirectToAction(nameof(Index), new { branchId = appointment.BranchId });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var appt = await _apiService.GetAppointmentAsync(id);
        if (appt == null) return NotFound();
        await _apiService.DeleteAppointmentAsync(id);
        return RedirectToAction(nameof(Index), new { branchId = appt.BranchId });
    }
}
