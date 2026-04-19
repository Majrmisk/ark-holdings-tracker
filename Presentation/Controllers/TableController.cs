using App.Models;
using Application.Interfaces;
using Application.Services;
using Domain;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Spectre.Console;

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
            return NotFound();
        }
        
        var roles = await userManager.GetRolesAsync(currentUser);
        
        Enum.TryParse(roles.First(), out UserRole role);
        
        var model = new TableViewModel
        {
            Diff = diff,
            ColorScheme = ColorService.GetColorSchemeByRole(role),
        };

        return View(model);
    }
}