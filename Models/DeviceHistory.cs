﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LoginWeb.Models;

namespace LoginWeb.Models 
{
    public class DeviceHistory
    {
        [Key]
        public long HistoryId { get; set; }

        [Required]
        public int DeviceId { get; set; } // Foreign Key linking back to the Device table

        [ForeignKey("DeviceId")]
        public virtual Device Device { get; set; } // Navigation property back to the parent Device

        [Required]
        public DateTime Timestamp { get; set; } 

        [Required]
        public bool WasOnline { get; set; } 

        public long? SysUpTimeSeconds { get; set; } 

        [Column(TypeName = "decimal(5, 2)")] 
        public decimal? CpuLoadPercentage { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal? MemoryUsagePercentage { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiskUsagePercentage { get; set; }

        public long? TotalRam{ get; set; } 
        public long? TotalDisk { get; set; }
        public long? UsedRamKBytes { get; set; }
        public long? UsedDiskKBytes { get; set; }

        [StringLength(50)]
        public string? PollingStatus { get; set; } // e.g., "Online", "SNMP Error", "Ping Failed"

        public DeviceHealth HealthStatus { get; set; } // The health status at the time of polling

        public string? HealthStatusReason { get; set; } // The reason at the time of polling
    }
}