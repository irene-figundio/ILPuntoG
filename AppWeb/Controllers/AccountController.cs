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

    [HttpGet]
    public async Task<IActionResult> GetGoogleAuthUrl(string redirectUri)
    {
        var url = await _apiService.GetGoogleAuthUrlAsync(redirectUri);
        return Json(new { authUrl = url });
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string code)
    {
        var redirectUri = Url.Action("GoogleCallback", "Account", null, Request.Scheme) ?? string.Empty;
        var response = await _apiService.GoogleSsoLoginAsync(code, redirectUri);

        if (response != null && response.Token != null)
        {
            await SignInWithToken(response.Token);
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Login", new { error = "Google SSO failed." });
    }

    private async Task SignInWithToken(string token)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var claims = jwtToken.Claims.ToList();
        claims.Add(new System.Security.Claims.Claim("Token", token));

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Cookies");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignInAsync(HttpContext, "Cookies", principal);

        Response.Cookies.Append("JwtToken", token, new CookieOptions { HttpOnly = true, Secure = Request.IsHttps });
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        var token = await _apiService.LoginAsync(username, password);
        if (token != null)
        {
            await SignInWithToken(token);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                return Ok(new { token });
            }
            return RedirectToAction("Index", "Home", new { token }); // Pass token in query for localStorage sync if needed
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Unauthorized();
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

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        await _apiService.ForgotPasswordAsync(email);
        // We show a generic confirmation message for security.
        ViewBag.Message = "Se l'email esiste, riceverai le istruzioni per reimpostare la password.";
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string username, string token)
    {
        ViewBag.Username = username;
        ViewBag.Token = token;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string username, string token, string newPassword)
    {
        var success = await _apiService.ResetPasswordAsync(username, token, newPassword);
        if (success)
        {
            return RedirectToAction("Login");
        }
        ViewBag.Error = "Reset fallito.";
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
