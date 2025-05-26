using LoginWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginWeb.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceHistory> DeviceHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            modelBuilder.Entity<Device>()
                .HasMany(d => d.Histories)
                .WithOne(h => h.Device)
                .HasForeignKey(h => h.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
