using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LoginWeb.Models;
using Microsoft.EntityFrameworkCore;
using LoginWeb.Data;
using Microsoft.AspNetCore.Authorization;
using LoginWeb.ViewModels;

namespace LoginWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetString("isLogin") == null)
        {
            return RedirectToAction("Login", "Account");
        }
        try
        {
            var devicesForView = await _context.Devices
                .Select(d => new DeviceDisplayViewModel
                {
                    // Assign properties from Device entity
                    Id = d.Id,
                    Name = d.Name,
                    IPAddress = d.IPAddress, 
                    Port = d.Port,
                    IsEnabled = d.IsEnabled,
                    LastStatus = d.LastStatus,
                    LastCheckTimestamp = d.LastCheckTimestamp,
                    OSVersion = d.OSVersion,
                    CommunityString = d.CommunityString,
                    PollingIntervalSeconds = d.PollingIntervalSeconds,

                    // Assign latest metrics from DeviceHistory
                    LatestSysUpTimeSeconds = d.Histories
                                           .OrderByDescending(h => h.Timestamp)
                                           .Select(h => h.SysUpTimeSeconds)
                                           .FirstOrDefault(),
                    // RAM Metrics 
                    LatestTotalRamKBytes = d.Histories
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.TotalRam) 
                                       .FirstOrDefault(),
                    LatestUsedRamKBytes = d.Histories 
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.UsedRamKBytes)
                                       .FirstOrDefault(),
                    LatestMemoryUsagePercentage = d.Histories
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.MemoryUsagePercentage)
                                       .FirstOrDefault(),

                    // Disk C Metrics
                    LatestTotalDiskCKBytes = d.Histories
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.TotalDiskCKBytes)
                                       .FirstOrDefault(),
                    LatestUsedDiskCKBytes = d.Histories
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.UsedDiskCKBytes) 
                                       .FirstOrDefault(),
                    LatestDiskCUsagePercentage = d.Histories
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.DiskCUsagePercentage)
                                       .FirstOrDefault(),
                    // Disk D Metrics
                    LatestTotalDiskDKBytes = d.Histories
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.TotalDiskCKBytes)
                                       .FirstOrDefault(),
                    LatestUsedDiskDKBytes = d.Histories
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.UsedDiskCKBytes)
                                       .FirstOrDefault(),
                    LatestDiskDUsagePercentage = d.Histories
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.DiskCUsagePercentage)
                                       .FirstOrDefault(),
                    // Disk E Metrics
                    LatestTotalDiskEKBytes = d.Histories
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.TotalDiskCKBytes)
                                       .FirstOrDefault(),
                    LatestUsedDiskEKBytes = d.Histories
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.UsedDiskCKBytes)
                                       .FirstOrDefault(),
                    LatestDiskEUsagePercentage = d.Histories
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.DiskCUsagePercentage)
                                       .FirstOrDefault(),
                    LatestCpuLoadPercentage = d.Histories
                                       .OrderByDescending(h => h.Timestamp)
                                       .Select(h => h.CpuLoadPercentage)
                                       .FirstOrDefault(),
                    HealthStatus = d.HealthStatus,
                    HealthStatusReason = d.HealthStatusReason,
                })
                .ToListAsync();

            return View(devicesForView); // Pass the list of ViewModels to the View
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching devices for Home/Index view.");
            // Pass an empty list or handle the error appropriately for the view
            return View(new List<DeviceDisplayViewModel>());
        }
    }
    public IActionResult About()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}
