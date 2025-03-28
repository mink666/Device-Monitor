using LoginWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LoginWeb.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser> // ✅ Inherit IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Device> Devices { get; set; } // ✅ Keep existing tables

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // ✅ Important for Identity

            modelBuilder.Entity<Device>()
                .Property(d => d.CPUUsage)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Device>()
                .Property(d => d.MemoryUsage)
                .HasColumnType("decimal(18,2)");
        }
    }
}
