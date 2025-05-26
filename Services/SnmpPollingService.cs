using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection; // Required for IServiceScopeFactory
using System;
using System.Threading;
using System.Threading.Tasks;
using LoginWeb.Data; // Your AppDbContext
using LoginWeb.Models; // Your Device, DeviceHistory models
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging; // For VersionCode, TimeoutException
using System.Net;
using System.Collections.Generic;
using System.Linq; // For .Where, .ToList, etc.
using Microsoft.EntityFrameworkCore; // For ToListAsync

// It's good practice to put services in a namespace
// namespace LoginWeb.Services { // For example

public class SnmpPollingService : IHostedService, IDisposable
{
    private readonly ILogger<SnmpPollingService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private Timer _timer;
    private readonly TimeSpan _defaultPollingIntervalSeconds = TimeSpan.FromSeconds(15);
     
    // Define your OIDs here
    private static readonly ObjectIdentifier SysUpTimeOid = new ObjectIdentifier("1.3.6.1.2.1.1.3.0");
    private static readonly ObjectIdentifier SysDescrOid = new ObjectIdentifier("1.3.6.1.2.1.1.1.0");
    private static readonly ObjectIdentifier TotalRamOid = new ObjectIdentifier("1.3.6.1.2.1.25.2.2.0"); // hrMemorySize

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
            // Using the class-level _logger. Can resolve a scoped one too if needed.

            List<Device> devicesToPotentiallyPoll; // Renamed for clarity
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
                // Check if it's time to poll this specific device based on its individual interval
                bool needsPolling = !device.LastCheckTimestamp.HasValue ||
                                    (DateTime.UtcNow - device.LastCheckTimestamp.Value).TotalSeconds >= device.PollingIntervalSeconds;

                if (!needsPolling)
                {
                    // _logger.LogTrace("Skipping poll for {DeviceName} ({IP}). Not time yet.", device.Name, device.IPAddress);
                    continue;
                }

                _logger.LogInformation("Polling device: {DeviceName} ({DeviceIP})", device.Name, device.IPAddress);
                bool pollSuccess = false;
                string? pollErrorMessage = null;

                DeviceHistory historyEntry = new DeviceHistory
                {
                    DeviceId = device.Id,
                    Timestamp = DateTime.UtcNow,
                    WasOnline = false // Assume offline until proven otherwise
                    // SysUpTimeSeconds, RawSystemDescription, TotalRamKBytes will be set if successful
                };

                try
                {
                    var endpoint = new IPEndPoint(IPAddress.Parse(device.IPAddress), device.Port);
                    var community = new OctetString(device.CommunityString);

                    // *** MODIFIED: Only include the three requested OIDs ***
                    var variablesToGet = new List<Variable>
                    {
                        new Variable(SysUpTimeOid),
                        new Variable(SysDescrOid),
                        new Variable(TotalRamOid) // Added Total RAM OID
                    };

                    VersionCode snmpVersionCode = device.SnmpVersion == SnmpVersionOption.V1 ? VersionCode.V1 : VersionCode.V2;

                    // *** MODIFIED: Use an integer timeout for GetAsync ***
                    // The overload with CancellationToken.None might not have a default timeout or use a very long one.
                    // It's better to use the overload that explicitly takes an int timeout in milliseconds.

                    IList<Variable> results = await Messenger.GetAsync(snmpVersionCode, endpoint, community, variablesToGet, CancellationToken.None);

                    // *** MODIFIED: Check if results is not null and if *any* variable was returned,
                    // as some OIDs might be valid while others are not.
                    // A more robust check would be `results.Count == variablesToGet.Count` if you expect all OIDs to exist.
                    // For now, if we get any valid data, consider it a partial success.
                    if (results != null && results.Any(v => !(v.Data is NoSuchInstance || v.Data is NoSuchObject || v.Data is EndOfMibView)))
                    {
                        pollSuccess = true; // Mark as successful if at least one OID returned valid data
                        historyEntry.WasOnline = true;
                        device.LastStatus = "Online";
                        device.LastErrorMessage = null;

                        // Process results
                        foreach (var variable in results)
                        {
                            if (variable.Data is NoSuchInstance || variable.Data is NoSuchObject || variable.Data is EndOfMibView)
                            {
                                _logger.LogWarning("SNMP OID {Oid} not found or at end of MIB on device {DeviceIP} ({DeviceName})", variable.Id, device.IPAddress, device.Name);
                                continue; // Skip this variable, but poll might still be a success overall
                            }

                            if (variable.Id.Equals(SysUpTimeOid) && variable.Data is TimeTicks uptime)
                            {
                                historyEntry.SysUpTimeSeconds = uptime.ToUInt32() / 100; // TimeTicks are in hundredths of a second
                                _logger.LogInformation("Device {DevName}: Uptime: {UpTime}s", device.Name, historyEntry.SysUpTimeSeconds);
                            }
                            else if (variable.Id.Equals(SysDescrOid) && variable.Data is OctetString sysDescrStr)
                            {
                                string description = sysDescrStr.ToString();
                                // *** ADDED: Store RawSystemDescription in historyEntry ***
                                if (historyEntry != null) // Should always be true here
                                {
                                    historyEntry.RawSystemDescription = description;
                                }
                                _logger.LogInformation("Device {DevName}: SysDescr: {Description}", device.Name, description);

                                // Optionally update Device.OSVersion if it's blank (as before)
                                if (string.IsNullOrEmpty(device.OSVersion))
                                {
                                    device.OSVersion = description.Length > 255 ? description.Substring(0, 255) : description;
                                    _logger.LogInformation("Updated OSVersion for {DevName} from SysDescr", device.Name);
                                }
                            }
                            // *** ADDED: Parsing for TotalRamOid ***
                            else if (variable.Id.Equals(TotalRamOid) && variable.Data is Integer32 ramSize) // hrMemorySize is Integer32
                            {
                                if (historyEntry != null) // Should always be true here
                                {
                                    historyEntry.TotalRamKBytes = ramSize.ToInt32();
                                }
                                _logger.LogInformation("Device {DevName}: Total RAM: {RAM} KBytes", device.Name, historyEntry.TotalRamKBytes);
                            }
                            // Removed processing for CpuIdleOid, MemTotalRealOid, MemAvailRealOid
                        }
                    }
                    else // This 'else' handles cases where results might be null or empty, or all OIDs error out.
                    {
                        _logger.LogWarning("SNMP poll to {DeviceIP} ({DeviceName}) returned no valid data or results array was null/empty.", device.IPAddress, device.Name);
                        pollErrorMessage = "No valid SNMP data received.";
                        // device.LastStatus will be set to Offline in finally if pollSuccess is false
                    }
                }
                catch (Lextm.SharpSnmpLib.Messaging.TimeoutException tex)
                {
                    _logger.LogWarning(tex, "SNMP timeout for device {DeviceName} ({DeviceIP})", device.Name, device.IPAddress);
                    device.LastStatus = "Timeout"; // Specific status for timeout
                    pollErrorMessage = "Timeout during SNMP request.";
                }
                catch (SnmpException snmpEx)
                {
                    _logger.LogWarning(snmpEx, "SNMP error polling device {DeviceName} ({DeviceIP})", device.Name, device.IPAddress);
                    // Set a more specific status based on the error
                    if (snmpEx.Message.ToLower().Contains("authorizationerror") || snmpEx.Message.ToLower().Contains("community"))
                    {
                        device.LastStatus = "AuthError";
                        pollErrorMessage = "Authentication Failure (Wrong Community String?)";
                    }
                    else
                    {
                        device.LastStatus = "SNMPError";
                        pollErrorMessage = $"SNMP Error: {snmpEx.Message}";
                    }
                }
                catch (FormatException formatEx) // Can happen if IPAddress.Parse fails
                {
                    _logger.LogError(formatEx, "Invalid IP Address format for device {DeviceName} ({DeviceIP})", device.Name, device.IPAddress);
                    device.LastStatus = "ConfigError";
                    pollErrorMessage = $"Invalid IP Address: {device.IPAddress}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Generic error polling device {DeviceName} ({DeviceIP})", device.Name, device.IPAddress);
                    device.LastStatus = "Error"; // More generic error status
                    pollErrorMessage = ex.Message;
                }
                finally // This block will always execute for the current device
                {
                    // Update Device status in DB
                    device.LastCheckTimestamp = DateTime.UtcNow;
                    if (pollSuccess)
                    {
                        // LastStatus already set to "Online" if pollSuccess is true
                    }
                    else
                    {
                        // If LastStatus wasn't set by a specific exception (Timeout, AuthError), set it to Offline.
                        if (string.IsNullOrEmpty(device.LastStatus) || device.LastStatus == "Online" /* if it was online before but failed now */)
                        {
                            device.LastStatus = "Offline";
                        }
                        device.LastErrorMessage = pollErrorMessage ?? "Polling failed (unknown reason).";
                    }

                    // Add history entry regardless of poll success to record the attempt
                    dbContext.DeviceHistories.Add(historyEntry);
                }
            } // end foreach device

            try
            {
                await dbContext.SaveChangesAsync(); // Save all changes (Device status updates and new DeviceHistory entries)
                _logger.LogInformation("Finished DoWork cycle. Changes saved to database.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SNMP Polling Service: Error saving changes to database after polling cycle.");
            }
        } // end using scope
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SNMP Polling Service stopping at {time}.", DateTimeOffset.Now);
        _timer?.Change(Timeout.Infinite, 0); // Stop the timer
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

// } // End namespace if you added one