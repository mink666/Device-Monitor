using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Needed for ToListAsync, FindAsync etc.
using System.Linq;
using LoginWeb.Models;
using LoginWeb.Data;
//using System.Security.Cryptography; // No longer needed here if EncryptionService is removed
//using System.Text; // No longer needed here
//using System; // Already implicitly available or via other usings
//using System.IO; // No longer needed if using [FromBody]
//using Org.BouncyCastle.Crypto.Digests; // No longer needed here
using System.Threading.Tasks; // Needed for async/await
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations; // For ILogger
using LoginWeb.DTOs;

namespace LoginWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Common API routing convention
    public class DeviceController : ControllerBase 
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DeviceController> _logger; 

        public DeviceController(AppDbContext context, ILogger<DeviceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Device
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetDevices()
        {
            try
            {
                var devicesFromDb = await _context.Devices
                    .Include(d => d.Histories.OrderByDescending(h => h.Timestamp).Take(1)) // Eager load latest history
                    .ToListAsync();

                var devicesData = devicesFromDb.Select(d =>
                {
                    DateTime? lastCheckTimestampUtc = null;
                    if (d.LastCheckTimestamp.HasValue)
                    {
                        // Crucially ensure the Kind is Utc IF it was stored as Utc
                        // If EF Core already retrieved it as Unspecified, specify it as Utc
                        // because you KNOW you stored it as DateTime.UtcNow
                        lastCheckTimestampUtc = DateTime.SpecifyKind(d.LastCheckTimestamp.Value, DateTimeKind.Utc);
                    }

                    return new
                    {
                        d.Id,
                        d.Name,
                        IpAddress = d.IPAddress,
                        d.Port,
                        d.IsEnabled,
                        d.LastStatus,
                        LastCheckTimestamp = lastCheckTimestampUtc, // Use the kind-specified version
                        d.OSVersion,
                        d.CommunityString,
                        d.PollingIntervalSeconds,
                        LatestSysUpTimeSeconds = d.Histories.FirstOrDefault()?.SysUpTimeSeconds,
                        LatestRawSystemDescription = d.Histories.FirstOrDefault()?.RawSystemDescription
                    };
                }).ToList();


                // Your logging snippet (now LastCheckTimestamp in firstDeviceForLog should have Kind = Utc)
                if (devicesData.Any())
                {
                    var firstDeviceForLog = devicesData.First();
                    var timestampPropertyInfo = firstDeviceForLog.GetType().GetProperty("LastCheckTimestamp");
                    if (timestampPropertyInfo != null)
                    {
                        var timestampValue = timestampPropertyInfo.GetValue(firstDeviceForLog) as DateTime?;
                        if (timestampValue.HasValue)
                        {
                            _logger.LogInformation("CONTROLLER - Sending Device '{DeviceName}', LastCheckTimestamp: {TimestampValueString}, Kind: {TimestampKind}",
                                firstDeviceForLog.GetType().GetProperty("Name")?.GetValue(firstDeviceForLog),
                                timestampValue.Value.ToString("o"),
                                timestampValue.Value.Kind);
                        }
                    }
                }
                return Ok(devicesData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching devices with latest polled data.");
                return StatusCode(500, new { success = false, message = "Internal Server Error fetching devices." });
            }
        }

        // GET: api/Device/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Device>> GetDevice(int id)
        {
            var device = await _context.Devices
                                     // .Include(d => d.Histories) // Optionally include history
                                     .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null)
            {
                return NotFound();
            }

            return device; // Consider a DeviceDetailDto to control returned data
        }


        // POST: api/Device
        [HttpPost]
        // [Authorize(Roles="Admin,User")] // Or just [Authorize]
        public async Task<ActionResult<Device>> CreateDevice([FromBody] DeviceCreateDto dto)
        {
            // Input validation is handled by [ApiController] and DTO attributes
            // You might add custom validation, e.g., check if IPAddress is unique if needed

            try
            {
                var newDevice = new Device
                {
                    Name = dto.Name,
                    IPAddress = dto.IpAddress, 
                    Port = dto.Port,
                    CommunityString = dto.CommunityString, 
                    IsEnabled = dto.IsEnabled,
                    PollingIntervalSeconds = dto.PollingIntervalSeconds,
                    OSVersion = dto.OSVersion,

                    // --- Default values ---
                    SnmpVersion = SnmpVersionOption.V2c, 
                    LastStatus = "Unknown", 
                    LastCheckTimestamp = null, 
                    LastErrorTimestamp = null,
                    LastErrorMessage = null

                    
                };
                _context.Devices.Add(newDevice);
                await _context.SaveChangesAsync();

                // Return 201 Created with location header and the created device
                return CreatedAtAction(nameof(GetDevice), new { id = newDevice.Id }, newDevice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating device."); // Use ILogger
                // Consider more specific error handling (e.g., DbUpdateException for uniqueness)
                return StatusCode(500, new { success = false, message = "Internal Server Error creating device." });
            }
        }

        // PUT: api/Device/5
        [HttpPut("{id}")] // Use HttpPut for replacing/updating resource
        // [Authorize(Roles="Admin,User")] // Or just [Authorize]
        public async Task<IActionResult> EditDevice(int id, [FromBody] DeviceEditDto dto)
        {
            var device = await _context.Devices.FindAsync(id);

            if (device == null)
            {
                _logger.LogWarning("EditDevice requested for non-existent Id: {DeviceId}", id);
                return NotFound(new { success = false, message = "Device not found." });
            }

            // --- Update properties from DTO ---
            device.Name = dto.Name;
            device.IPAddress = dto.IPAddress; 
            device.Port = dto.Port;
            device.CommunityString = dto.CommunityString; 
            device.IsEnabled = dto.IsEnabled;
            device.PollingIntervalSeconds = dto.PollingIntervalSeconds;
            device.OSVersion = dto.OSVersion;

            _context.Entry(device).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Device updated successfully: {DeviceId}", id);
                return NoContent(); // Standard successful PUT response
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error occurred while editing device Id: {DeviceId}", id);
                // Handle concurrency conflict if necessary
                return Conflict(new { success = false, message = "Concurrency error updating device." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while editing device Id: {DeviceId}", id);
                return StatusCode(500, new { success = false, message = "Internal Server Error updating device." });
            }
        }

        // DELETE: api/Device/5
        [HttpDelete("{id}")]
        // [Authorize(Roles="Admin,User")] // Or just [Authorize]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                _logger.LogWarning("DeleteDevice requested for non-existent Id: {DeviceId}", id);
                return NotFound(new { success = false, message = "Device not found." });
            }

            // --- Consider related DeviceHistory records ---
            // By default, if your FK relationship isn't set to Cascade Delete,
            // history records will NOT be deleted, which might be desired.
            // If you WANT to delete history: configure Cascade Delete in OnModelCreating
            // or query and remove history records manually here (less efficient).
            // Example:
            // var history = await _context.DeviceHistories.Where(h => h.DeviceId == id).ToListAsync();
            // _context.DeviceHistories.RemoveRange(history);

            _context.Devices.Remove(device);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Device deleted successfully: {DeviceId}", id);
                return NoContent(); // Standard successful DELETE response
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting device Id: {DeviceId}", id);
                return StatusCode(500, new { success = false, message = "Internal Server Error deleting device." });
            }
        }


        // --- REMOVED EncryptionService ---
        // The EncryptionService inner class is removed as it's no longer needed
        // for the SNMPv2c CommunityString approach. If you implement encryption
        // at rest for CommunityString later, use ASP.NET Core Data Protection
        // or a similar standard mechanism, likely injected as a service.

    }
}