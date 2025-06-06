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

public class SnmpPollingService : IHostedService, IDisposable
{
    private readonly ILogger<SnmpPollingService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private Timer _timer;
    private readonly TimeSpan _defaultPollingIntervalSeconds = TimeSpan.FromSeconds(15);


    private static readonly ObjectIdentifier SysUpTimeOid = new ObjectIdentifier("1.3.6.1.2.1.1.3.0");
    private static readonly ObjectIdentifier SysDescrOid = new ObjectIdentifier("1.3.6.1.2.1.1.1.0");
    private static readonly ObjectIdentifier CpuLoadOid = new ObjectIdentifier("1.3.6.1.2.1.25.3.3.1.2.3");

    private static readonly ObjectIdentifier RamAllocUnitsOid = new ObjectIdentifier("1.3.6.1.2.1.25.2.3.1.4.4");
    private static readonly ObjectIdentifier RamTotalSizeOid = new ObjectIdentifier("1.3.6.1.2.1.25.2.3.1.5.4");
    private static readonly ObjectIdentifier RamUsedSizeOid = new ObjectIdentifier("1.3.6.1.2.1.25.2.3.1.6.4");

    private static readonly ObjectIdentifier DiskCAllocUnitsOid = new ObjectIdentifier("1.3.6.1.2.1.25.2.3.1.4.1");
    private static readonly ObjectIdentifier DiskCTotalSizeOid = new ObjectIdentifier("1.3.6.1.2.1.25.2.3.1.5.1");
    private static readonly ObjectIdentifier DiskCUsedSizeOid = new ObjectIdentifier("1.3.6.1.2.1.25.2.3.1.6.1");

    public SnmpPollingService(ILogger<SnmpPollingService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SNMP Polling Service starting at {time}.", DateTimeOffset.Now);
        _timer = new Timer(DoWork, null, TimeSpan.Zero, _defaultPollingIntervalSeconds);
        return Task.CompletedTask;
    }

    private async void DoWork(object state)
    {
        _logger.LogInformation("SNMP Polling Service executing DoWork cycle at {time}.", DateTimeOffset.Now);

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<Device> devicesToPotentiallyPoll;
            try
            {
                devicesToPotentiallyPoll = await dbContext.Devices.Where(d => d.IsEnabled).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SNMP Polling Service: Error fetching devices from database.");
                return;
            }

            foreach (var device in devicesToPotentiallyPoll)
            {
                bool needsPolling = !device.LastCheckTimestamp.HasValue ||
                                    (DateTime.UtcNow - device.LastCheckTimestamp.Value).TotalSeconds >= device.PollingIntervalSeconds;

                if (!needsPolling)
                {
                    continue;
                }

                _logger.LogInformation("Polling device: {DeviceName} ({DeviceIP})", device.Name, device.IPAddress);
                bool pollSuccess = false;
                string? pollErrorMessage = null;

                DeviceHistory historyEntry = new DeviceHistory
                {
                    DeviceId = device.Id,
                    Timestamp = DateTime.UtcNow,
                    WasOnline = false
                };

                try
                {
                    var endpoint = new IPEndPoint(IPAddress.Parse(device.IPAddress), device.Port);
                    var community = new OctetString(device.CommunityString);
                    _logger.LogInformation("Device {DevName} ({DevIP}): Community created: '{Community}'", device.Name, device.IPAddress, device.CommunityString);

                    var variablesToGet = new List<Variable>
                    {
                        new Variable(SysUpTimeOid),
                        new Variable(SysDescrOid),
                        new Variable(CpuLoadOid),
                        new Variable(RamAllocUnitsOid),
                        new Variable(RamTotalSizeOid),
                        new Variable(RamUsedSizeOid),
                        new Variable(DiskCAllocUnitsOid),
                        new Variable(DiskCTotalSizeOid),
                        new Variable(DiskCUsedSizeOid)
                    };
                    _logger.LogInformation("Device {DevName} ({DevIP}): Variables to get list created. Count: {VarCount}", device.Name, device.IPAddress, variablesToGet.Count);

                    VersionCode snmpVersionCode = device.SnmpVersion == SnmpVersionOption.V1 ? VersionCode.V1 : VersionCode.V2;

                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    _logger.LogInformation("Device {DevName}: Sending SNMP GET request with 10-second timeout.", device.Name);
                    IList<Variable> results = await Messenger.GetAsync(snmpVersionCode, endpoint, community, variablesToGet, cts.Token);
                    _logger.LogInformation("Device {DevName}: SNMP GET call completed. Results count: {Count}", device.Name, results?.Count ?? 0);

                    if (results != null && results.Count > 0 && results.Any(v => !(v.Data is NoSuchInstance || v.Data is NoSuchObject || v.Data is EndOfMibView)))
                    {
                        pollSuccess = true;
                        historyEntry.WasOnline = true;
                        //device.LastStatus = "Online";
                        //device.LastErrorMessage = null;

                        long? ramAllocUnitsVal = null, ramTotalUnitsVal = null, ramUsedUnitsVal = null;
                        long? diskCAllocUnitsVal = null, diskCTotalUnitsVal = null, diskCUsedUnitsVal = null;
                        int? cpuLoadVal = null;

                        foreach (var variable in results)
                        {
                            if (variable.Data is NoSuchInstance || variable.Data is NoSuchObject || variable.Data is EndOfMibView)
                            {
                                _logger.LogWarning("SNMP OID {Oid} not found or at end of MIB on device {DeviceIP} ({DeviceName})", variable.Id, device.IPAddress, device.Name);
                                continue;
                            }

                            if (variable.Id.Equals(SysUpTimeOid) && variable.Data is TimeTicks uptime)
                            {
                                historyEntry.SysUpTimeSeconds = uptime.ToUInt32() / 100;
                                _logger.LogInformation("Device {DevName}: Uptime: {UpTime}s", device.Name, historyEntry.SysUpTimeSeconds);
                            }
                            else if (variable.Id.Equals(SysDescrOid) && variable.Data is OctetString sysDescrStr)
                            {
                                string newDescription = sysDescrStr.ToString();
                                _logger.LogInformation("Device {DevName}: SysDescr received (length {Length}): {DescriptionSample}",
                                    device.Name,
                                    newDescription.Length,
                                    newDescription.Substring(0, Math.Min(newDescription.Length, 70)) + (newDescription.Length > 70 ? "..." : ""));
                                string osVersionToStore = newDescription.Length > 255 ? newDescription.Substring(0, 255) : newDescription;
                                if (device.OSVersion != osVersionToStore)
                                {
                                    device.OSVersion = osVersionToStore;
                                    _logger.LogInformation("Updated device.OSVersion for {DevName} with content from SysDescr.", device.Name);
                                }
                            }
                            else if (variable.Id.Equals(CpuLoadOid) && variable.Data is Integer32 cpuLoad)
                            {
                                cpuLoadVal = cpuLoad.ToInt32();
                            }
                            else if (variable.Id.Equals(RamAllocUnitsOid) && variable.Data is Integer32 ramAllocValResult) { ramAllocUnitsVal = ramAllocValResult.ToInt32(); }
                            else if (variable.Id.Equals(RamTotalSizeOid) && variable.Data is Integer32 ramTotalValResult) { ramTotalUnitsVal = ramTotalValResult.ToInt32(); }
                            else if (variable.Id.Equals(RamUsedSizeOid) && variable.Data is Integer32 ramUsedValResult) { ramUsedUnitsVal = ramUsedValResult.ToInt32(); }
                            else if (variable.Id.Equals(DiskCAllocUnitsOid) && variable.Data is Integer32 diskCAllocValResult) { diskCAllocUnitsVal = diskCAllocValResult.ToInt32(); }
                            else if (variable.Id.Equals(DiskCTotalSizeOid) && variable.Data is Integer32 diskCTotalValResult) { diskCTotalUnitsVal = diskCTotalValResult.ToInt32(); }
                            else if (variable.Id.Equals(DiskCUsedSizeOid) && variable.Data is Integer32 diskCUsedValResult) { diskCUsedUnitsVal = diskCUsedValResult.ToInt32(); }
                        }

                        if (cpuLoadVal.HasValue)
                        {
                            historyEntry.CpuLoadPercentage = cpuLoadVal.Value;
                            _logger.LogInformation("Device {DevName}: CPU Load: {CPULoad}%", device.Name, historyEntry.CpuLoadPercentage);
                        }
                        else
                        {
                            _logger.LogWarning("Device {DevName}: CPU Load data not retrieved or invalid.", device.Name);
                        }

                        if (ramAllocUnitsVal.HasValue && ramTotalUnitsVal.HasValue && ramUsedUnitsVal.HasValue &&
                            ramAllocUnitsVal.Value > 0 && ramTotalUnitsVal.Value > 0)
                        {
                            long totalRamBytes = ramTotalUnitsVal.Value * ramAllocUnitsVal.Value;
                            long usedRamBytes = ramUsedUnitsVal.Value * ramAllocUnitsVal.Value;
                            historyEntry.TotalRam = totalRamBytes / 1024;
                            historyEntry.UsedRamKBytes = usedRamBytes / 1024;
                            if (totalRamBytes > 0)
                            {
                                historyEntry.MemoryUsagePercentage = Math.Round(((decimal)usedRamBytes / totalRamBytes) * 100, 2);
                            }
                            _logger.LogInformation("Device {DevName}: RAM Usage: {RAMPercent:F2}% ({UsedKB}KB / {TotalKB}KB)",
                                device.Name, historyEntry.MemoryUsagePercentage, usedRamBytes / 1024, totalRamBytes / 1024);
                        }
                        else
                        {
                            _logger.LogWarning("Device {DevName}: Missing some RAM metrics (AllocUnits, Total, Used) for percentage calculation or values were invalid.", device.Name);
                        }

                        if (diskCAllocUnitsVal.HasValue && diskCTotalUnitsVal.HasValue && diskCUsedUnitsVal.HasValue &&
                            diskCAllocUnitsVal.Value > 0 && diskCTotalUnitsVal.Value > 0)
                        {
                            long totalDiskCBytes = diskCTotalUnitsVal.Value * diskCAllocUnitsVal.Value;
                            long usedDiskCBytes = diskCUsedUnitsVal.Value * diskCAllocUnitsVal.Value;
                            historyEntry.TotalDisk = totalDiskCBytes / 1024;
                            historyEntry.UsedDiskKBytes = usedDiskCBytes / 1024;
                            if (totalDiskCBytes > 0)
                            {
                                historyEntry.DiskUsagePercentage = Math.Round(((decimal)usedDiskCBytes / totalDiskCBytes) * 100, 2);
                            }
                            _logger.LogInformation("Device {DevName}: Disk C: Usage: {DiskPercent:F2}% ({UsedKB}KB / {TotalKB}KB)",
                                device.Name, historyEntry.DiskUsagePercentage, usedDiskCBytes / 1024, totalDiskCBytes / 1024);
                        }
                        else
                        {
                            _logger.LogWarning("Device {DevName}: Missing some Disk C: metrics (AllocUnits, Total, Used) for percentage calculation or values were invalid.", device.Name);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Device {DevName}: No valid SNMP data received.", device.Name);
                        pollErrorMessage = "No valid SNMP data received.";
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("SNMP request timed out for device {DeviceName} ({DeviceIP}) after 10 seconds.", device.Name, device.IPAddress);
                    device.LastStatus = "Timeout";
                    pollErrorMessage = "Timeout during SNMP request.";
                }
                catch (SnmpException snmpEx)
                {
                    _logger.LogWarning(snmpEx, "SNMP error polling device {DeviceName} ({DeviceIP}): {ErrorDetails}", device.Name, device.IPAddress, snmpEx.Message);
                    device.LastStatus = snmpEx.Message.Contains("authorizationerror") ? "AuthError" : "SNMPError";
                    pollErrorMessage = $"SNMP Error: {snmpEx.Message}";
                }
                catch (FormatException formatEx)
                {
                    _logger.LogError(formatEx, "Invalid IP Address format for device {DeviceName} ({DeviceIP})", device.Name, device.IPAddress);
                    device.LastStatus = "ConfigError";
                    pollErrorMessage = $"Invalid IP Address: {device.IPAddress}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error polling device {DeviceName} ({DeviceIP}): {ErrorDetails}", device.Name, device.IPAddress, ex.Message);
                    device.LastStatus = "Error";
                    pollErrorMessage = ex.Message;
                }
                finally
                {
                    _logger.LogInformation("Device {DevName}: Entering finally block. Poll success: {Success}. Status: {Status}. Error: {Error}",
                        device.Name, pollSuccess, device.LastStatus, pollErrorMessage);

                    device.LastCheckTimestamp = DateTime.UtcNow;
                    if (!pollSuccess)
                    {
                        if (string.IsNullOrEmpty(device.LastStatus) || device.LastStatus == "Online")
                        {
                            device.LastStatus = "Offline";
                        }
                        device.LastErrorMessage = pollErrorMessage ?? "Polling failed (unknown reason).";
                        device.HealthStatus = DeviceHealth.Unreachable;
                        device.HealthStatusReason = device.LastErrorMessage;
                    }
                    else // pollSuccess is true
                    {
                        device.LastStatus = "Online"; 
                        device.LastErrorMessage = null;

                        device.HealthStatus = DeviceHealth.Healthy;
                        device.HealthStatusReason = "Device is online and responding normally.";
                    }
                    dbContext.DeviceHistories.Add(historyEntry);
                }
            } 

            try
            {
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Finished DoWork cycle. Changes saved to database.");
            }
            catch (DbUpdateException dbEx) 
            {
                _logger.LogError(dbEx, "SNMP Polling Service: Database error saving changes after polling cycle.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SNMP Polling Service: Error saving changes to database after polling cycle.");
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