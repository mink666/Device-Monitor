using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using LoginWeb.Data; 
using LoginWeb.Models; 
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging; 
using System.Net;
using System.Collections.Generic;
using System.Linq; 
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using LoginWeb.Services;

public class SnmpPollingService : IHostedService, IDisposable
{
    private readonly ILogger<SnmpPollingService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private Timer _timer;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(1);
    private readonly IConfiguration _configuration;

    //private const decimal CpuWarningThreshold = 80.0m;
    //private const decimal RamWarningThreshold = 80.0m;
    //private const decimal DiskWarningThreshold = 80.0m;
    //private const int EmailAlertMinutes = 5;

    private static readonly ObjectIdentifier SysUpTimeOid = new ObjectIdentifier("1.3.6.1.2.1.1.3.0");
    private static readonly ObjectIdentifier SysDescrOid = new ObjectIdentifier("1.3.6.1.2.1.1.1.0");
    private static readonly ObjectIdentifier CpuLoadOid = new ObjectIdentifier("1.3.6.1.2.1.25.3.3.1.2.3");

    public SnmpPollingService(ILogger<SnmpPollingService> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.Zero, _pollingInterval);
        return Task.CompletedTask;
    }
    private async void DoWork(object state)
    {
        _logger.LogInformation("SNMP Polling Service executing DoWork cycle at {time}.", DateTimeOffset.Now);

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var devicesToPoll = await dbContext.Devices.Where(d => d.IsEnabled).ToListAsync();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var globalCpuThreshold = _configuration.GetValue<decimal>("MonitoringSettings:DefaultCpuWarningThreshold", 80);
            var globalRamThreshold = _configuration.GetValue<decimal>("MonitoringSettings:DefaultRamWarningThreshold", 85);
            var globalDiskThreshold = _configuration.GetValue<decimal>("MonitoringSettings:DefaultDiskWarningThreshold", 80);
            var emailAlertMinutes = _configuration.GetValue<int>("MonitoringSettings:EmailAlertWarningMinutes", 5);

            foreach (var device in devicesToPoll)
            {
                bool isDueForPoll = false;
                if (!device.LastCheckTimestamp.HasValue)
                {
                    // This is a new device that has never been polled. Poll it now.
                    isDueForPoll = true;
                }
                else
                {
                    // This is an existing device. Check if the interval has passed.
                    if ((DateTime.UtcNow - device.LastCheckTimestamp.Value).TotalSeconds >= device.PollingIntervalSeconds)
                    {
                        isDueForPoll = true;
                    }
                }

                if (!isDueForPoll)
                {
                    // If it's not due for a poll, skip to the next device in the loop.
                    continue;
                }

                _logger.LogInformation("Polling device: {DeviceName} ({DeviceIP})", device.Name, device.IPAddress);

                var historyEntry = new DeviceHistory { DeviceId = device.Id, Timestamp = DateTime.UtcNow };
                bool pollSuccess = false;
                string pollErrorMessage = null;

                try
                {
                    var endpoint = new IPEndPoint(IPAddress.Parse(device.IPAddress), device.Port);
                    var community = new OctetString(device.CommunityString);
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                    // 1. Discover Indexes if they don't exist yet for this device
                    if (!device.RamStorageIndex.HasValue || !device.DiskCStorageIndex.HasValue)
                    {
                        _logger.LogInformation("Performing one-time index discovery for {DeviceName}...", device.Name);
                        var storageDescTableOid = new ObjectIdentifier("1.3.6.1.2.1.25.2.3.1.3");
                        var discoveryResults = new List<Variable>();
                        await Messenger.WalkAsync(VersionCode.V2, endpoint, community, storageDescTableOid, discoveryResults, WalkMode.WithinSubtree, new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token);

                        foreach (var variable in discoveryResults)
                        {
                            var description = variable.Data.ToString();
                            var index = (int)variable.Id.ToNumerical().Last();

                            if (description.Equals("Physical Memory", StringComparison.OrdinalIgnoreCase))
                                device.RamStorageIndex = index;
                            else if (description.Contains("C:\\"))
                                device.DiskCStorageIndex = index;
                            else if (description.Contains("D:\\"))
                                device.DiskDStorageIndex = index;
                            else if (description.Contains("E:\\"))
                                device.DiskEStorageIndex = index;
                        }
                        await dbContext.SaveChangesAsync(); // Save the discovered indexes immediately
                        _logger.LogInformation("Discovery for {DeviceName} complete. RAM Idx: {RamIdx}, C: Idx: {CIdx}, D: Idx: {DIdx}, E: Idx: {EIdx}",
                            device.Name, device.RamStorageIndex, device.DiskCStorageIndex, device.DiskDStorageIndex, device.DiskEStorageIndex);
                    }

                    // 2. Build the list of OIDs to poll for this cycle
                    var variablesToGet = new List<Variable> { new Variable(SysUpTimeOid), new Variable(SysDescrOid), new Variable(CpuLoadOid) };

                    // This dictionary is the key to reliably processing the results
                    var oidFriendlyNameMap = new Dictionary<string, string>();

                    if (device.RamStorageIndex.HasValue)
                    {
                        var idx = device.RamStorageIndex.Value;
                        string oid;

                        oid = $"1.3.6.1.2.1.25.2.3.1.4.{idx}"; variablesToGet.Add(new Variable(new ObjectIdentifier(oid))); oidFriendlyNameMap[oid] = "RamAllocUnits";
                        oid = $"1.3.6.1.2.1.25.2.3.1.5.{idx}"; variablesToGet.Add(new Variable(new ObjectIdentifier(oid))); oidFriendlyNameMap[oid] = "RamTotalSize";
                        oid = $"1.3.6.1.2.1.25.2.3.1.6.{idx}"; variablesToGet.Add(new Variable(new ObjectIdentifier(oid))); oidFriendlyNameMap[oid] = "RamUsedSize";
                    }
                    if (device.DiskCStorageIndex.HasValue)
                    {
                        var idx = device.DiskCStorageIndex.Value;
                        string oid;

                        oid = $"1.3.6.1.2.1.25.2.3.1.4.{idx}"; variablesToGet.Add(new Variable(new ObjectIdentifier(oid))); oidFriendlyNameMap[oid] = "DiskCAllocUnits";
                        oid = $"1.3.6.1.2.1.25.2.3.1.5.{idx}"; variablesToGet.Add(new Variable(new ObjectIdentifier(oid))); oidFriendlyNameMap[oid] = "DiskCTotalSize";
                        oid = $"1.3.6.1.2.1.25.2.3.1.6.{idx}"; variablesToGet.Add(new Variable(new ObjectIdentifier(oid))); oidFriendlyNameMap[oid] = "DiskCUsedSize";
                    }
                    if (device.DiskDStorageIndex.HasValue)
                    {
                        _logger.LogInformation("Found Disk D Index ({Index}). Adding OIDs to poll list.", device.DiskDStorageIndex.Value);

                        var idx = device.DiskDStorageIndex.Value;
                        string oid;
                        oid = $"1.3.6.1.2.1.25.2.3.1.4.{idx}"; variablesToGet.Add(new Variable(new ObjectIdentifier(oid))); oidFriendlyNameMap[oid] = "DiskDAllocUnits";
                        oid = $"1.3.6.1.2.1.25.2.3.1.5.{idx}"; variablesToGet.Add(new Variable(new ObjectIdentifier(oid))); oidFriendlyNameMap[oid] = "DiskDTotalSize";
                        oid = $"1.3.6.1.2.1.25.2.3.1.6.{idx}"; variablesToGet.Add(new Variable(new ObjectIdentifier(oid))); oidFriendlyNameMap[oid] = "DiskDUsedSize";
                    }
                    if (device.DiskEStorageIndex.HasValue)
                    {
                        var idx = device.DiskEStorageIndex.Value;
                        string oid;
                        oid = $"1.3.6.1.2.1.25.2.3.1.4.{idx}"; variablesToGet.Add(new Variable(new ObjectIdentifier(oid))); oidFriendlyNameMap[oid] = "DiskEAllocUnits";
                        oid = $"1.3.6.1.2.1.25.2.3.1.5.{idx}"; variablesToGet.Add(new Variable(new ObjectIdentifier(oid))); oidFriendlyNameMap[oid] = "DiskETotalSize";
                        oid = $"1.3.6.1.2.1.25.2.3.1.6.{idx}"; variablesToGet.Add(new Variable(new ObjectIdentifier(oid))); oidFriendlyNameMap[oid] = "DiskEUsedSize";
                    }
                    // 3. Perform SNMP GET
                    VersionCode snmpVersionCode = device.SnmpVersion == SnmpVersionOption.V1 ? VersionCode.V1 : VersionCode.V2;
                    var snmpResults = await Messenger.GetAsync(snmpVersionCode, endpoint, community, variablesToGet, cts.Token);

                    // 4. Process Results using the Dictionary
                    if (snmpResults != null && snmpResults.Count > 0 && snmpResults.Any(v => !(v.Data is NoSuchInstance)))
                    {
                        pollSuccess = true;
                        historyEntry.WasOnline = true;

                        long? ramAlloc = null, ramTotal = null, ramUsed = null;
                        long? diskCAlloc = null, diskCTotal = null, diskCUsed = null;
                        long? diskDAlloc = null, diskDTotal = null, diskDUsed = null;
                        long? diskEAlloc = null, diskETotal = null, diskEUsed = null;

                        foreach (var variable in snmpResults)
                        {
                            if (variable.Data is NoSuchInstance) continue;

                            if (variable.Id.Equals(SysUpTimeOid) && variable.Data is TimeTicks uptime) historyEntry.SysUpTimeSeconds = uptime.ToUInt32() / 100;
                            else if (variable.Id.Equals(SysDescrOid) && variable.Data is OctetString desc) device.OSVersion = desc.ToString();
                            else if (variable.Id.Equals(CpuLoadOid) && variable.Data is Integer32 cpu) historyEntry.CpuLoadPercentage = cpu.ToInt32();
                            else if (oidFriendlyNameMap.TryGetValue(variable.Id.ToString(), out var friendlyName))
                            {
                                var value = ((Integer32)variable.Data).ToInt32();
                                switch (friendlyName)
                                {
                                    case "RamAllocUnits": ramAlloc = value; break;
                                    case "RamTotalSize": ramTotal = value; break;
                                    case "RamUsedSize": ramUsed = value; break;
                                    case "DiskCAllocUnits": diskCAlloc = value; break;
                                    case "DiskCTotalSize": diskCTotal = value; break;
                                    case "DiskCUsedSize": diskCUsed = value; break;
                                    case "DiskDAllocUnits": diskDAlloc = value; break;
                                    case "DiskDTotalSize": diskDTotal = value; break;
                                    case "DiskDUsedSize": diskDUsed = value; break;
                                    case "DiskEAllocUnits": diskEAlloc = value; break;
                                    case "DiskETotalSize": diskETotal = value; break;
                                    case "DiskEUsedSize": diskEUsed = value; break;
                                }
                            }
                        }

                        // Calculate percentages from the retrieved values
                        if (ramAlloc.HasValue && ramTotal.HasValue && ramUsed.HasValue && ramTotal > 0 && ramAlloc > 0)
                        {
                            long totalRamBytes = ramTotal.Value * ramAlloc.Value;
                            long usedRamBytes = ramUsed.Value * ramAlloc.Value;
                            historyEntry.TotalRam = totalRamBytes / 1024;
                            historyEntry.UsedRamKBytes = usedRamBytes / 1024;
                            historyEntry.MemoryUsagePercentage = Math.Round(((decimal)usedRamBytes / totalRamBytes) * 100, 2);
                        }
                        // Handle Disk C
                        if (diskCAlloc.HasValue && diskCTotal.HasValue && diskCUsed.HasValue && diskCTotal > 0 && diskCAlloc > 0)
                        {
                            long totalDiskBytes = diskCTotal.Value * diskCAlloc.Value;
                            long usedDiskBytes = diskCUsed.Value * diskCAlloc.Value;
                            historyEntry.TotalDiskCKBytes = totalDiskBytes / 1024; // Use new property name
                            historyEntry.UsedDiskCKBytes = usedDiskBytes / 1024;    // Use new property name
                            historyEntry.DiskCUsagePercentage = Math.Round(((decimal)usedDiskBytes / totalDiskBytes) * 100, 2);
                        }

                        // Handle Disk D
                        if (diskDAlloc.HasValue && diskDTotal.HasValue && diskDUsed.HasValue && diskDTotal > 0 && diskDAlloc > 0)
                        {
                            long totalDiskBytes = diskDTotal.Value * diskDAlloc.Value;
                            long usedDiskBytes = diskDUsed.Value * diskDAlloc.Value;
                            historyEntry.TotalDiskDKBytes = totalDiskBytes / 1024; // Use new property name
                            historyEntry.UsedDiskDKBytes = usedDiskBytes / 1024;    // Use new property name
                            historyEntry.DiskDUsagePercentage = Math.Round(((decimal)usedDiskBytes / totalDiskBytes) * 100, 2);
                        }

                        // Handle Disk E
                        if (diskEAlloc.HasValue && diskETotal.HasValue && diskEUsed.HasValue && diskETotal > 0 && diskEAlloc > 0)
                        {
                            long totalDiskBytes = diskETotal.Value * diskEAlloc.Value;
                            long usedDiskBytes = diskEUsed.Value * diskEAlloc.Value;
                            historyEntry.TotalDiskEKBytes = totalDiskBytes / 1024; // Use new property name
                            historyEntry.UsedDiskEKBytes = usedDiskBytes / 1024;    // Use new property name
                            historyEntry.DiskEUsagePercentage = Math.Round(((decimal)usedDiskBytes / totalDiskBytes) * 100, 2);
                        }
                    }
                    else
                    {
                        pollErrorMessage = "No valid SNMP data received.";
                    }
                }
                catch (OperationCanceledException)
                {
                    // This catches timeouts specifically.
                    _logger.LogWarning("SNMP request timed out for device {DeviceName}", device.Name);
                    pollSuccess = false;
                    device.LastStatus = "Timeout";
                    pollErrorMessage = "The device did not respond in time (Timeout).";
                }
                catch (SnmpException snmpEx)
                {
                    // This catches SNMP-specific errors, like a wrong community string.
                    _logger.LogWarning(snmpEx, "SNMP error for device {DeviceName}", device.Name);
                    pollSuccess = false;
                    device.LastStatus = "SNMP Error";
                    pollErrorMessage = $"SNMP Protocol Error: {snmpEx.Message}";
                }
                catch (Exception ex)
                {
                    // This is a general catch-all for any other unexpected errors (e.g., invalid IP format).
                    _logger.LogError(ex, "An unexpected error occurred polling device {DeviceName}", device.Name);
                    pollSuccess = false;
                    device.LastStatus = "Error";
                    pollErrorMessage = $"An unexpected error occurred: {ex.Message}";
                }

                device.LastCheckTimestamp = DateTime.UtcNow;

                DeviceHealth newStatus;
                string newReason = null;
                var warningReasons = new List<string>();
                if (pollSuccess)
                {
                    var cpuThreshold = device.CpuWarningThreshold ?? globalCpuThreshold;
                    if (historyEntry.CpuLoadPercentage > cpuThreshold) warningReasons.Add($"High CPU: {historyEntry.CpuLoadPercentage:F2}%");

                    var ramThreshold = device.RamWarningThreshold ?? globalRamThreshold;
                    if (historyEntry.MemoryUsagePercentage > ramThreshold) warningReasons.Add($"High RAM: {historyEntry.MemoryUsagePercentage:F2}%");

                    var diskThreshold = device.DiskWarningThreshold ?? globalDiskThreshold;
                    if (historyEntry.DiskCUsagePercentage > diskThreshold) warningReasons.Add($"High Disk C: {historyEntry.DiskCUsagePercentage:F2}%");
                    if (historyEntry.DiskDUsagePercentage > diskThreshold) warningReasons.Add($"High Disk D: {historyEntry.DiskDUsagePercentage:F2}%");
                    if (historyEntry.DiskEUsagePercentage > diskThreshold) warningReasons.Add($"High Disk E: {historyEntry.DiskEUsagePercentage:F2}%");

                    if (warningReasons.Any())
                    {
                        newStatus = DeviceHealth.Warning;
                        newReason = string.Join(", ", warningReasons);
                    }
                    else
                    {
                        newStatus = DeviceHealth.Healthy;
                        newReason = "Device is online and responding normally.";
                    }
                }
                else
                {
                    newStatus = DeviceHealth.Unreachable;
                    newReason = pollErrorMessage;
                }
                device.LastStatus = pollSuccess ? "Online" : "Offline";
                device.HealthStatus = newStatus;
                device.HealthStatusReason = newReason;

                // Handle Web Notifications
                if (newStatus == DeviceHealth.Warning)
                {
                    await hubContext.Clients.All.SendAsync("ReceiveWarning", device.Name, newReason);
                }

                // Handle Email Alert Logic
                if (newStatus == DeviceHealth.Warning)
                {
                    if (!device.WarningStateTimestamp.HasValue)
                    {
                        device.WarningStateTimestamp = DateTime.UtcNow;
                    }
                    else if ((DateTime.UtcNow - device.WarningStateTimestamp.Value).TotalMinutes > emailAlertMinutes)
                    {
                        var recipient = configuration.GetValue<string>("EmailSettings:AlertRecipientEmail");
                        if (!string.IsNullOrEmpty(recipient))
                        {
                            var subject = $"[Device Monitor] Persistent Warning: {device.Name}";
                            await emailService.SendEmailAsync(recipient, subject, $"Reason: {newReason}");
                        }
                        device.WarningStateTimestamp = DateTime.UtcNow; // Reset timer to prevent spam
                    }
                }
                else // If device is Healthy or Unreachable, clear the warning timer
                {
                    device.WarningStateTimestamp = null;
                }
                historyEntry.PollingStatus = device.LastStatus;
                historyEntry.HealthStatus = newStatus;
                historyEntry.HealthStatusReason = newReason;
                dbContext.DeviceHistories.Add(historyEntry);
                await dbContext.SaveChangesAsync();
            }
        }
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SNMP Polling Service stopping at {time}.", DateTimeOffset.Now);
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}