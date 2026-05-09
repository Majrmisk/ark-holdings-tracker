using App.Models;
using Application.Interfaces;
using Application.Services;
using Domain;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

public class TableController(UserManager<AppUser> userManager, IHoldingService holdingService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var diff = await holdingService.GetLatestDiffAsync();

        if (diff == null)
        {
            return NotFound();
        }

        var currentUser = await userManager.GetUserAsync(User);

        if (currentUser == null)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(currentUser);

        Enum.TryParse(roles.FirstOrDefault(), out UserRole role);

        var model = new TableViewModel
        {
            Diff = diff,
            ColorScheme = ColorService.GetColorSchemeByRole(role),
        };

        return View(model);
    }
}
