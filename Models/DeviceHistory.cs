using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginWeb.Models 
{
    public class DeviceHistory
    {
        [Key]
        public long HistoryId { get; set; } // Use long for potentially many records

        [Required]
        public int DeviceId { get; set; } // Foreign Key linking back to the Device table

        [ForeignKey("DeviceId")] // Configure the foreign key relationship
        public virtual Device Device { get; set; } // Navigation property back to the parent Device

        [Required]
        public DateTime Timestamp { get; set; } // The exact time this data was recorded

        [Required]
        public bool WasOnline { get; set; } // Records if the device was successfully polled at this time

        // --- Metrics ---
        // Make these nullable ('?') if the data might not always be available
        // or if you only record them when WasOnline is true.

        public long? SysUpTimeSeconds { get; set; } // System uptime stored consistently (e.g., total seconds)

        [Column(TypeName = "decimal(5, 2)")] // Example: Store CPU as decimal(5,2) e.g., 999.99 - Adjust precision as needed
        public decimal? CpuLoadPercentage { get; set; }

        [Column(TypeName = "decimal(5, 2)")] // Example: Store Memory as decimal(5,2) - Adjust precision as needed
        public decimal? MemoryUsagePercentage { get; set; }

        public int? BatteryLevel { get; set; } // Battery level percentage, if applicable

        public string? RawSystemDescription { get; set; } // To store the full sysDescr
        public long? TotalRamKBytes { get; set; }
        // Add other metrics you plan to poll here, e.g.:
        // public decimal? Temperature { get; set; }
        // public long? DiskUsedBytes { get; set; }
        // public long? DiskTotalBytes { get; set; }
    }
}