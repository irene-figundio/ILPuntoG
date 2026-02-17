using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Threading.Tasks;

namespace AppWeb.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class VacationsController : BaseController
{
    public VacationsController(ApiService apiService) : base(apiService)
    {
    }

    public async Task<IActionResult> Index()
    {
        var vacations = await _apiService.GetVacationsAsync();
        return View(vacations);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Vacation vacation)
    {
        await _apiService.AddVacationAsync(vacation);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _apiService.DeleteVacationAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
