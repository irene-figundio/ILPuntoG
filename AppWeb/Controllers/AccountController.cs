using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppWeb.Controllers;

public class AccountController : Controller
{
    private readonly ApiService _apiService;

    public AccountController(ApiService apiService)
    {
        _apiService = apiService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        var token = await _apiService.LoginAsync(username, password);
        if (token != null)
        {
            Response.Cookies.Append("JwtToken", token, new CookieOptions { HttpOnly = true, Secure = true });
            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Invalid login attempt.";
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string name, string email, string username, string password)
    {
        var success = await _apiService.RegisterAsync(name, email, username, password);
        if (success)
        {
            return RedirectToAction("Login");
        }

        ViewBag.Error = "Registration failed.";
        return View();
    }

    public IActionResult Logout()
    {
        Response.Cookies.Delete("JwtToken");
        return RedirectToAction("Index", "Home");
    }
}
