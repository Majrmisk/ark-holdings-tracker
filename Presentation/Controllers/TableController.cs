using App.Models;
using Application.Interfaces;
using Application.Services;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

[Authorize]
public class TableController(IAuthService authService, IHoldingService holdingService) : Controller
{
    public async Task<IActionResult> Index(bool colors = true)
    {
        var diff = await holdingService.GetLatestDiffAsync();

        if (diff == null)
            return View(new TableViewModel());

        var role = await authService.GetCurrentUserRoleAsync(User);
        var canToggle = role == UserRole.PowerUser;

        var model = new TableViewModel
        {
            Diff = diff,
            ColorScheme = colors ? ColorService.GetColorSchemeByRole(role) : new Colorscheme(),
            CanToggleColors = canToggle,
            ColorsEnabled = colors,
        };

        return View(model);
    }
}
