// In LoginWeb/ViewModels/DeviceDisplayViewModel.cs
using System;
using System.Collections.Generic; // If you ever embed history directly

namespace LoginWeb.ViewModels // Or LoginWeb.Models if you prefer
{
    public class DeviceDisplayViewModel
    {
        // Properties from Device model
        public int Id { get; set; }
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public bool IsEnabled { get; set; }
        public string? LastStatus { get; set; }
        public DateTime? LastCheckTimestamp { get; set; }
        public string? OSVersion { get; set; }
        public string CommunityString { get; set; } 
        public int PollingIntervalSeconds { get; set; }

        //Latest Records
        public long? LatestSysUpTimeSeconds { get; set; }

        // RAM Metrics
        public long? LatestTotalRamKBytes { get; set; }      
        public long? LatestUsedRamKBytes { get; set; }        
        public decimal? LatestMemoryUsagePercentage { get; set; }

        // Disk C Metrics 
        public long? LatestTotalDiskCKBytes { get; set; }
        public long? LatestUsedDiskCKBytes { get; set; }
        public decimal? LatestDiskCUsagePercentage { get; set; }
        // --- Disk D Metrics (New) ---
        public long? LatestTotalDiskDKBytes { get; set; }
        public long? LatestUsedDiskDKBytes { get; set; }
        public decimal? LatestDiskDUsagePercentage { get; set; }

        // --- Disk E Metrics (New) ---
        public long? LatestTotalDiskEKBytes { get; set; }
        public long? LatestUsedDiskEKBytes { get; set; }
        public decimal? LatestDiskEUsagePercentage { get; set; }
        public decimal? LatestCpuLoadPercentage { get; set; }
        // Health Status 
        public Models.DeviceHealth HealthStatus { get; set; }
        public string? HealthStatusReason { get; set; }
    }
}