using App.Models;
using Application.Interfaces;
using Application.Services;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Spectre.Console;

namespace App.Controllers;

public class TableController : Controller
{
    private readonly IHoldingService _holdingService;
    
    public TableController(IHoldingService holdingService)
    {
        _holdingService = holdingService;
    }

    public async Task<IActionResult> Index()
    {
        var diff = await _holdingService.GetLatestDiffAsync();

        if (diff == null)
        {
            return NotFound();
        }

        var model = new TableViewModel
        {
            Diffs = [diff],
            ColorScheme = ColorService.GetSuperAdminColors()
        };

        return View(model);
    }
}