using LoginWeb.Data;
using LoginWeb.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
public class DeviceHealthAnalyzerService : BackgroundService
{
    private readonly ILogger<DeviceHealthAnalyzerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _analysisInterval = TimeSpan.FromMinutes(5); 

    // THRESHOLDS 
    private const decimal CpuWarningThreshold = 80.0m; 
    private const decimal RamWarningThreshold = 85.0m; 
    private const decimal DiskWarningThreshold = 80.0m; 
    private const int StaleDataThresholdMinutes = 15; 

    public DeviceHealthAnalyzerService(ILogger<DeviceHealthAnalyzerService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Device Health Analyzer Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_analysisInterval, stoppingToken); // Wait for the 5-minute interval
                _logger.LogInformation("Device Health Analyzer is running its check at {time}", DateTimeOffset.Now);

                await AnalyzeDeviceHealth();
            }
            catch (OperationCanceledException)
            {
                // This is expected when the application is stopping.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in the Device Health Analyzer Service.");
            }
        }

        _logger.LogInformation("Device Health Analyzer Service is stopping.");
    }

    private async Task AnalyzeDeviceHealth()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

            // Get all devices that are currently enabled for monitoring.
            // We include their latest history record for analysis.
            var devicesToAnalyze = await dbContext.Devices
                .Where(d => d.IsEnabled)
                .Include(d => d.Histories.OrderByDescending(h => h.Timestamp).Take(1))
                .ToListAsync();

            foreach (var device in devicesToAnalyze)
            {
                // The main SNMP Poller handles setting the status to 'Offline'.
                // This analyzer's job is to check 'Healthy' devices for potential 'Warning' conditions.
                // It can also check if a supposedly healthy device has gone stale.
                if (device.HealthStatus == DeviceHealth.Unreachable)
                {
                    continue; // Skip analysis if the device is already known to be offline.
                }

                var latestHistory = device.Histories.FirstOrDefault();
                string newWarningReason = null;

                // --- CHECK 1: Disconnectivity / Stale Data ---
                // Is the data for a supposedly 'Healthy' device very old?
                if (device.LastCheckTimestamp.HasValue && (DateTime.UtcNow - device.LastCheckTimestamp.Value).TotalMinutes > StaleDataThresholdMinutes)
                {
                    newWarningReason = $"Data is stale. Last successful poll was at {device.LastCheckTimestamp.Value.ToShortTimeString()}.";
                }
                // --- CHECK 2: Performance Thresholds ---
                // This check only runs if the first check didn't find an issue and we have history.
                else if (latestHistory != null)
                {
                    if (latestHistory.CpuLoadPercentage.HasValue && latestHistory.CpuLoadPercentage > CpuWarningThreshold)
                    {
                        newWarningReason = $"High CPU Usage: {latestHistory.CpuLoadPercentage:F2}% (Threshold: >{CpuWarningThreshold}%)";
                    }
                    else if (latestHistory.MemoryUsagePercentage.HasValue && latestHistory.MemoryUsagePercentage > RamWarningThreshold)
                    {
                        newWarningReason = $"High Memory Usage: {latestHistory.MemoryUsagePercentage:F2}% (Threshold: >{RamWarningThreshold}%)";
                    }
                    else if (latestHistory.DiskUsagePercentage.HasValue && latestHistory.DiskUsagePercentage > DiskWarningThreshold)
                    {
                        newWarningReason = $"High Disk Usage: {latestHistory.DiskUsagePercentage:F2}% (Threshold: >{DiskWarningThreshold}%)";
                    }
                    // You can add more 'else if' checks here for data spikes later.
                }

                // --- UPDATE DEVICE HEALTH STATUS ---
                if (newWarningReason != null)
                {
                    device.HealthStatus = DeviceHealth.Warning;
                    device.HealthStatusReason = newWarningReason;
                    _logger.LogWarning("Device '{DeviceName}' moved to WARNING state. Reason: {Reason}", device.Name, newWarningReason);
                    await hubContext.Clients.All.SendAsync("ReceiveWarning", device.Name, newWarningReason);

                }
                else
                {
                    device.HealthStatus = DeviceHealth.Healthy;
                    device.HealthStatusReason = "Device is online and responding normally.";
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}