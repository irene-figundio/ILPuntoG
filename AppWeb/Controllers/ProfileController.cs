using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AppWeb.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize]
public class ProfileController : Controller
{
    private readonly ApiService _apiService;

    public ProfileController(ApiService apiService)
    {
        _apiService = apiService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "Le password non coincidono.";
            return View("Index");
        }

        var success = await _apiService.ChangePasswordAsync(oldPassword, newPassword);
        if (success)
        {
            ViewBag.Message = "Password aggiornata con successo.";
        }
        else
        {
            ViewBag.Error = "Errore durante l'aggiornamento. Verifica la vecchia password.";
        }
        return View("Index");
    }
}
