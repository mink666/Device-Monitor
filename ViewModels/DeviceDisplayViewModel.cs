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
        public string CommunityString { get; set; } // For data-* attributes
        public int PollingIntervalSeconds { get; set; } // For data-* attributes

        // New properties from DeviceHistory (latest record)
        public long? LatestSysUpTimeSeconds { get; set; }
    }
}