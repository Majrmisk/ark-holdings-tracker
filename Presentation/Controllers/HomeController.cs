using App.Models;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace App.Controllers;

public class HomeController : Controller
{
    private readonly ILogger _logger;
    private readonly IHoldingService _holdingService;

    public HomeController(ILogger<HomeController> logger, IHoldingService holdingService)
    {
        _logger = logger;
        _holdingService = holdingService;
    }

    public async Task<IActionResult> Index()
    {
        var snapshot = await _holdingService.GetLatestSnapshotAsync();
        var diff = await _holdingService.GetLatestDiffAsync();
        if (snapshot == null)
        {
            _logger.LogInformation("None latest snapshot");
        }
        else
        {
            var log = new StringBuilder();
            log.AppendLine($"Snapshot {snapshot.Id} | Fetched at {snapshot.FetchedAt:yyyy-MM-dd HH:mm:ss} | Holdings: {snapshot.Holdings.Count}");
            log.AppendLine();
            log.AppendLine($"  {"Ticker",-8} {"Company",-40} {"CUSIP",-12} {"Shares",12} {"Market Value (USD)",18} {"Weight",8}");
            log.AppendLine($"  {new string('-', 104)}");
            foreach (var h in snapshot.Holdings)
                log.AppendLine($"  {h.Ticker ?? "N/A",-8} {h.Company,-40} {h.Cusip,-12} {h.Shares,12:N0} {h.MarketValueUsd,18:N2} {h.WeightPercent,7:F2}%");

            _logger.LogInformation(log.ToString());
        }
        if (diff == null)
        {
            _logger.LogInformation("No latest diff");
        }
        else
        {
            var log = new StringBuilder();
            log.AppendLine($"Diff {diff.Id} | Computed at {diff.ComputedAt:yyyy-MM-dd HH:mm:ss} | {diff.PreviousSnapshotId} -> {diff.CurrentSnapshotId} | Entries: {diff.Entries.Count}");
            log.AppendLine();
            log.AppendLine($"  {"Type",-10} {"Ticker",-8} {"Company",-40} {"CUSIP",-12} {"Shares",12} {"Weight",8} {"Change%",10}");
            log.AppendLine($"  {new string('-', 106)}");
            foreach (var e in diff.Entries)
                log.AppendLine($"  {e.Type,-10} {e.Ticker ?? "N/A",-8} {e.Company,-40} {e.Cusip,-12} {e.Shares,12:N0} {e.WeightPercent,7:F2}% {(e.ChangePercent.HasValue ? $"{e.ChangePercent.Value,9:F2}%" : "N/A"),10}");

            _logger.LogInformation(log.ToString());
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        if (statusCode.HasValue)
        {
            ViewData["StatusCode"] = statusCode.Value;
            Response.StatusCode = statusCode.Value;
        }
        else if (ViewData["StatusCode"] == null)
        {
            Response.StatusCode = 404;
        }

        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}