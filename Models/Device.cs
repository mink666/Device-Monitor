using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoginWeb.Models
{
    public class Device
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }

        public string IPAddress { get; set; }

        public string Status { get; set; }

        public string? DeviceType { get; set; }
        public decimal? CPUUsage { get; set; }
        public decimal? MemoryUsage { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int? BatteryLevel { get; set; }
        public string? OSVersion { get; set; }

        public int? Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string? IV { get; set; }
    }

    public class DeviceUpdateModel
    {
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string Status { get; set; }
    }
}
