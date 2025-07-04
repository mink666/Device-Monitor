﻿using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using LoginWeb.Data;
using LoginWeb.Models;
using Microsoft.AspNetCore.Authorization;
using LoginWeb.ViewModels;
using System.Threading.Tasks; // Required for async operations
using Microsoft.EntityFrameworkCore; // Required for Include and ToListAsync

public class DeviceHistoryReportViewModel
{
    public string DeviceName { get; set; }
    public string DeviceIpAddress { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<DeviceHistory> HistoryRecords { get; set; }
}

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

                LatestUsedDiskCKBytes = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.UsedDiskCKBytes).FirstOrDefault(),
                LatestTotalDiskCKBytes = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.TotalDiskCKBytes).FirstOrDefault(),
                LatestDiskCUsagePercentage = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.DiskCUsagePercentage).FirstOrDefault(),

                LatestUsedDiskDKBytes = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.UsedDiskCKBytes).FirstOrDefault(),
                LatestTotalDiskDKBytes = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.TotalDiskCKBytes).FirstOrDefault(),
                LatestDiskDUsagePercentage = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.DiskCUsagePercentage).FirstOrDefault(),

                LatestUsedDiskEKBytes = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.UsedDiskCKBytes).FirstOrDefault(),
                LatestTotalDiskEKBytes = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.TotalDiskCKBytes).FirstOrDefault(),
                LatestDiskEUsagePercentage = d.Histories.OrderByDescending(h => h.Timestamp).Select(h => h.DiskCUsagePercentage).FirstOrDefault(),

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
    // =======================================================================
    //                  BEGIN: New Historical Report Action
    // =======================================================================
    [HttpGet]
    public async Task<IActionResult> GenerateHistoryReport(int deviceId, string title, DateTime startDate, DateTime endDate)
    {
        var username = HttpContext.Session.GetString("Username");
        if (HttpContext.Session.GetString("isLogin") == null)
        {
            return Unauthorized("You must be logged in to access reports.");
        }

        // --- Fetch Device and its specific history in the date range ---
        var device = await _context.Devices.FindAsync(deviceId);
        if (device == null)
        {
            return NotFound("The specified device could not be found.");
        }

        var historyRecords = await _context.DeviceHistories
            .Where(h => h.DeviceId == deviceId && h.Timestamp >= startDate && h.Timestamp <= endDate)
            .OrderBy(h => h.Timestamp) // Order chronologically
            .ToListAsync();

        if (!historyRecords.Any())
        {
            return BadRequest("No historical data found for the selected device in the specified date range.");
        }

        // --- Prepare the ViewModel for the report service ---
        var reportViewModel = new DeviceHistoryReportViewModel
        {
            DeviceName = device.Name,
            DeviceIpAddress = device.IPAddress,
            StartDate = startDate,
            EndDate = endDate,
            HistoryRecords = historyRecords
        };

        // --- Call the new report service method ---
        byte[] pdfBytes = PdfReportService.GenerateDeviceHistoryReport(title, reportViewModel, username);

        // Return the generated PDF file
        return File(pdfBytes, "application/pdf");
    }
    // =======================================================================
    //                   END: New Historical Report Action
    // =======================================================================
}
