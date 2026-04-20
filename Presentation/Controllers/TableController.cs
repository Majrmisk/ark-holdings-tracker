using App.Models;
using Application.Interfaces;
using Application.Services;
using Domain;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using PuppeteerSharp.Media;
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

    public async Task<IActionResult> ExportPdf()
    {
        var url = Url.Action("Index", "Table", null, Request.Scheme);
        if (url == null)
        {
            return NotFound();
        }

        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        await using var page = await browser.NewPageAsync();

        var cookieName = ".AspNetCore.Identity.Application";
        if (Request.Cookies.TryGetValue(cookieName, out var cookieValue))
        {
            await page.SetCookieAsync(new CookieParam
            {
                Name = cookieName,
                Value = cookieValue,
                Domain = Request.Host.Host
            });
        }

        await page.GoToAsync(url, WaitUntilNavigation.Networkidle0);

        var pdfStream = await page.PdfStreamAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions
            {
                Top = "15px",
                Bottom = "15px",
                Left = "5px",
                Right = "5px"
            },
            Scale = 0.8m,
        });

        var memoryStream = new MemoryStream();
        await pdfStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var fileName = $"HoldingDiff_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

        Response.Headers.Append("Content-Disposition", $"inline; filename={fileName}");
        return File(memoryStream, "application/pdf");
    }
}