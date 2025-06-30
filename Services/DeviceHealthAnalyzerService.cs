//using LoginWeb.Data;
//using LoginWeb.Models;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.EntityFrameworkCore;
//using LoginWeb.Services;
//public class DeviceHealthAnalyzerService : BackgroundService
//{
//    private readonly ILogger<DeviceHealthAnalyzerService> _logger;
//    private readonly IServiceScopeFactory _scopeFactory;

//    private readonly TimeSpan _analysisInterval = TimeSpan.FromMinutes(3);

//    // THRESHOLDS 
//    private const decimal CpuWarningThreshold = 80.0m;
//    private const decimal RamWarningThreshold = 80.0m;
//    private const decimal DiskWarningThreshold = 80.0m;
//    private const int StaleDataThresholdMinutes = 15; 

//    public DeviceHealthAnalyzerService(ILogger<DeviceHealthAnalyzerService> logger, IServiceScopeFactory scopeFactory)
//    {
//        _logger = logger;
//        _scopeFactory = scopeFactory;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("Device Health Analyzer Service is starting.");

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                await Task.Delay(_analysisInterval, stoppingToken); // Wait for the 5-minute interval
//                _logger.LogInformation("Device Health Analyzer is running its check at {time}", DateTimeOffset.Now);

//                await AnalyzeDeviceHealth();
//            }
//            catch (OperationCanceledException)
//            {
//                // This is expected when the application is stopping.
//                break;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "An unexpected error occurred in the Device Health Analyzer Service.");
//            }
//        }

//        _logger.LogInformation("Device Health Analyzer Service is stopping.");
//    }

//    private async Task AnalyzeDeviceHealth()
//    {
//        using (var scope = _scopeFactory.CreateScope())
//        {
//            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
//            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
//            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>(); // Get configuration

//            // Get all devices that are currently enabled for monitoring.
//            // We include their latest history record for analysis.
//            var devicesToAnalyze = await dbContext.Devices
//                .Where(d => d.IsEnabled)
//                .Include(d => d.Histories.OrderByDescending(h => h.Timestamp).Take(1))
//                .ToListAsync();

//            foreach (var device in devicesToAnalyze)
//            {
//                var latestHistory = device.Histories.FirstOrDefault();
//                var oldStatus = device.HealthStatus;
//                DeviceHealth newStatus;
//                string newWarningReason = null;

//                // The main SNMP Poller handles setting the status to 'Offline'.
//                // This analyzer's job is to check 'Healthy' devices for potential 'Warning' conditions.
//                // It can also check if a supposedly healthy device has gone stale.
//                if (device.HealthStatus == DeviceHealth.Unreachable)
//                {
//                    continue; // Skip analysis if the device is already known to be offline.
//                }

//                // --- CHECK 1: Disconnectivity / Stale Data ---
//                // Is the data for a supposedly 'Healthy' device very old?
//                if (device.LastCheckTimestamp.HasValue && (DateTime.UtcNow - device.LastCheckTimestamp.Value).TotalMinutes > StaleDataThresholdMinutes)
//                {
//                    newWarningReason = $"Data is stale. Last successful poll was at {device.LastCheckTimestamp.Value.ToShortTimeString()}.";
//                }
//                // --- CHECK 2: Performance Thresholds ---
//                // This check only runs if the first check didn't find an issue and we have history.
//                else if (latestHistory != null)
//                {
//                    var cpuThreshold = device.CpuWarningThreshold ?? CpuWarningThreshold;
//                    var ramThreshold = device.RamWarningThreshold ?? RamWarningThreshold;
//                    var diskThreshold = device.DiskWarningThreshold ?? DiskWarningThreshold;

//                    if (latestHistory.CpuLoadPercentage.HasValue && latestHistory.CpuLoadPercentage > cpuThreshold)
//                    {
//                        newWarningReason = $"High CPU Usage: {latestHistory.CpuLoadPercentage:F2}% (Threshold: >{cpuThreshold}%)";
//                    }
//                    else if (latestHistory.MemoryUsagePercentage.HasValue && latestHistory.MemoryUsagePercentage > ramThreshold)
//                    {
//                        newWarningReason = $"High Memory Usage: {latestHistory.MemoryUsagePercentage:F2}% (Threshold: >{ramThreshold}%)";
//                    }
//                    else if (latestHistory.DiskCUsagePercentage.HasValue && latestHistory.DiskCUsagePercentage > diskThreshold)
//                    {
//                        newWarningReason = $"High Disk C Usage: {latestHistory.DiskCUsagePercentage:F2}% (Threshold: >{diskThreshold}%)";
//                    }
//                    else if (latestHistory.DiskDUsagePercentage.HasValue && latestHistory.DiskDUsagePercentage > diskThreshold)
//                    {
//                        newWarningReason = $"High Disk D Usage: {latestHistory.DiskDUsagePercentage:F2}% (Threshold: >{diskThreshold}%)";
//                    }
//                    else if (latestHistory.DiskEUsagePercentage.HasValue && latestHistory.DiskEUsagePercentage > diskThreshold)
//                    {
//                        newWarningReason = $"High Disk E Usage: {latestHistory.DiskEUsagePercentage:F2}% (Threshold: >{diskThreshold}%)";
//                    }
//                }

//                // --- UPDATE DEVICE HEALTH STATUS ---
//                if (newWarningReason != null)
//                {
//                    newStatus = DeviceHealth.Warning;
//                }
//                else
//                {
//                    newStatus = DeviceHealth.Healthy;
//                    newWarningReason = "Device is online and responding normally.";
//                }

//                device.HealthStatus = newStatus;
//                device.HealthStatusReason = newWarningReason;

//                if (newStatus == DeviceHealth.Warning)
//                {
//                    // Send web notification every time a warning is detected.
//                    await hubContext.Clients.All.SendAsync("ReceiveWarning", device.Name, newWarningReason);

//                    // Send email alert only on the initial state change to prevent spam.
//                    if (oldStatus != DeviceHealth.Warning)
//                    {
//                        _logger.LogWarning("Device '{Name}' has transitioned to WARNING. Sending email alert.", device.Name);

//                        var recipientEmail = configuration.GetValue<string>("EmailSettings:AlertRecipientEmail");
//                        if (!string.IsNullOrEmpty(recipientEmail))
//                        {
//                            var emailSubject = $"[SmarTrack] Warning: {device.Name} needs attention";
//                            var emailBody = $"<p>A warning has been triggered...</p><p><strong>Reason:</strong> {newWarningReason}</p>";

//                            await emailService.SendEmailAsync(recipientEmail, emailSubject, emailBody);
//                        }
//                    }
//                }

//            }

//            await dbContext.SaveChangesAsync();
//        }
//    }
//}