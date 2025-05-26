using System.ComponentModel.DataAnnotations;

namespace LoginWeb.DTOs
{
    public class DeviceCreateDto 
    {
        [Required] public string Name { get; set; }
        [Required] public string IpAddress { get; set; }
        [Required] public int Port { get; set; }
        [Required] public string CommunityString { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int PollingIntervalSeconds { get; set; }
        public string? OSVersion { get; set; }
    }
}
