using App.Models;
using Application.Interfaces;
using Application.Services;
using Domain;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

public class TableController(IAuthService authService, IHoldingService holdingService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var diff = await holdingService.GetLatestDiffAsync();

        if (diff == null)
        {
            return NotFound();
        }

        var role = await authService.GetCurrentUserRoleAsync(User);

        var model = new TableViewModel
        {
            Diff = diff,
            ColorScheme = ColorService.GetColorSchemeByRole(role),
        };

        return View(model);
    }
}
