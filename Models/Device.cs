using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginWeb.Models
{
    public enum SnmpVersionOption
    {
        V1 = 0,
        V2c = 1,
        V3 = 3
    }
    public enum DeviceHealth
    {
        Unknown = 0, 
        Healthy = 1,
        Warning = 2,
        Unreachable = 3
    }
    public class Device
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(255)]
        public string IPAddress { get; set; }

        [Required]
        public int Port { get; set; }

        public bool IsEnabled { get; set; } = true;
        public int PollingIntervalSeconds { get; set; } = 60;
        public string? OSVersion { get; set; }

        [StringLength(50)]
        public string? LastStatus { get; set; }
        public DateTime? LastCheckTimestamp { get; set; }
        public DateTime? LastErrorTimestamp { get; set; }
        public string? LastErrorMessage { get; set; }

        public DeviceHealth HealthStatus { get; set; } = DeviceHealth.Unknown;
        public string? HealthStatusReason { get; set; }
        // --- SNMP v2c Configuration ---
        public SnmpVersionOption SnmpVersion { get; set; } = SnmpVersionOption.V2c;

        [Required]
        [StringLength(100)]
        public string CommunityString { get; set; }
        public int? RamStorageIndex { get; set; } // To store the discovered index for Physical Memory
        public int? DiskCStorageIndex { get; set; } // To store the discovered index for the C: drive
        public int? DiskDStorageIndex { get; set; }
        public int? DiskEStorageIndex { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal? CpuWarningThreshold { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal? RamWarningThreshold { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal? DiskWarningThreshold { get; set; }
        public virtual ICollection<DeviceHistory> Histories { get; set; } = new List<DeviceHistory>();

        //public string Username { get; set; }
        //public string Password { get; set; }
        //public string? IV { get; set; }
    }
}
