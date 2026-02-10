using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace AppWeb.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class ClientsController : Controller
{
    private readonly ApiService _apiService;
    public ClientsController(ApiService apiService) { _apiService = apiService; }

    public async Task<IActionResult> Index()
    {
        var clients = await _apiService.GetClientsAsync();
        return View(clients);
    }

    public async Task<IActionResult> Details(int id)
    {
        var client = await _apiService.GetClientAsync(id);
        if (client == null) return NotFound();
        return View(client);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(Client client)
    {
        await _apiService.CreateClientAsync(client);
        return RedirectToAction(nameof(Index));
    }
}
