using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using LoginWeb.Data;
using LoginWeb.Models;
using Microsoft.AspNetCore.Authorization;
using LoginWeb.ViewModels;
using System.Threading.Tasks; // Required for async operations
using Microsoft.EntityFrameworkCore; // Required for Include and ToListAsync
public class ReportsController : Controller
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context) 
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Generate(string title)
    {
        var username = HttpContext.Session.GetString("Username");
        if (HttpContext.Session.GetString("isLogin") == null)
        {
            return Unauthorized("You must be logged in to access reports.");
        }

        // --- Efficient Data Fetching ---
        // Fetch all devices and their LATEST history in a single, efficient query,
        // projecting the results directly into your DeviceDisplayViewModel.
        var devicesForReport = await _context.Devices
            .Where(d => d.IsEnabled) // Only include enabled devices
            .Select(d => new DeviceDisplayViewModel
            {
                Name = d.Name,
                IPAddress = d.IPAddress,
                IsEnabled = d.IsEnabled,
                LastStatus = d.LastStatus,
                LastCheckTimestamp = d.LastCheckTimestamp,
                LatestSysUpTimeSeconds = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.SysUpTimeSeconds).FirstOrDefault(),
                HealthStatus = d.HealthStatus,
                LatestUsedRamKBytes = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.UsedRamKBytes).FirstOrDefault(),
                LatestTotalRamKBytes = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.TotalRam).FirstOrDefault(),
                LatestMemoryUsagePercentage = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.MemoryUsagePercentage).FirstOrDefault(),
                LatestUsedDiskKBytes = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.UsedDiskKBytes).FirstOrDefault(),
                LatestTotalDiskKBytes = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.TotalDisk).FirstOrDefault(),
                LatestDiskUsagePercentage = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.DiskUsagePercentage).FirstOrDefault(),
                LatestCpuLoadPercentage = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.CpuLoadPercentage).FirstOrDefault()
            })
            .ToListAsync();


        if (devicesForReport == null || !devicesForReport.Any())
        {
            // You can return a simple message or even generate a PDF stating "No devices found"
            return BadRequest("No devices available to generate a report.");
        }

        // --- Call the Report Service with the Prepared Data ---
        // Note that we no longer pass '_context' to the report generator.
        // It's also expecting a List<DeviceDisplayViewModel> now.
        byte[] pdfBytes = PdfReportService.GenerateDeviceReport(title, devicesForReport, username);

        return File(pdfBytes, "application/pdf");
    }

    // You can add your action for the detailed history report here later
    // [HttpGet]
    // public async Task<IActionResult> GenerateDetailReport(int deviceId, DateTime startDate, DateTime endDate) { ... }
}