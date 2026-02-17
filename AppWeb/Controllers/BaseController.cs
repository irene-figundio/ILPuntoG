using AppWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Models;

namespace AppWeb.Controllers;

public abstract class BaseController : Controller
{
    protected readonly ApiService _apiService;

    protected BaseController(ApiService apiService)
    {
        _apiService = apiService;
    }

    protected int? CurrentBranchId { get; private set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var branches = await _apiService.GetBranchesAsync();
            ViewBag.AllBranches = branches;

            if (Request.Cookies.TryGetValue("SelectedBranchId", out var branchIdStr) && int.TryParse(branchIdStr, out var branchId))
            {
                if (branchId == 0) // "Tutte le Sedi"
                {
                    CurrentBranchId = null;
                    ViewBag.CurrentBranchName = "Tutte le Sedi";
                }
                else
                {
                    var selectedBranch = branches.FirstOrDefault(b => b.Id == branchId);
                    if (selectedBranch != null)
                    {
                        CurrentBranchId = branchId;
                        ViewBag.CurrentBranchName = selectedBranch.Name;
                    }
                    else
                    {
                        CurrentBranchId = null;
                        ViewBag.CurrentBranchName = "Tutte le Sedi";
                    }
                }
            }
            else
            {
                CurrentBranchId = null;
                ViewBag.CurrentBranchName = "Tutte le Sedi";
            }
        }

        await next();
    }
}
