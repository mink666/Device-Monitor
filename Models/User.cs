// Models/User.cs
using System.ComponentModel.DataAnnotations;

namespace LoginWeb.Models
{
    public class User
    {
        [Key] public int Id { get; set; }
        [Required][StringLength(100)] public string Username { get; set; }
        [Required][EmailAddress][StringLength(256)] public string Email { get; set; }
        [Required] public string PasswordHash { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool EmailConfirmed { get; set; } = false;
        public bool AccountStatus { get; set; } = true;
    }
}