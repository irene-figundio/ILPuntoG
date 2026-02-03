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
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claims = jwtToken.Claims.ToList();
            claims.Add(new System.Security.Claims.Claim("Token", token));

            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Cookies");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignInAsync(HttpContext, "Cookies", principal);

            // Also keep the token cookie for ApiService
            Response.Cookies.Append("JwtToken", token, new CookieOptions { HttpOnly = true, Secure = Request.IsHttps });

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

    public async Task<IActionResult> Logout()
    {
        await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(HttpContext, "Cookies");
        Response.Cookies.Delete("JwtToken");
        Response.Cookies.Delete("AppAuth");
        return RedirectToAction("Index", "Home");
    }
}
