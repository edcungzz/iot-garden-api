using Microsoft.EntityFrameworkCore;
using IoTGardenApi.Models;

namespace IoTGardenApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // New schema tables
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<Sensor> Sensors { get; set; } = null!;
    public DbSet<SensorLogData> SensorLogData { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure relationships
        modelBuilder.Entity<Sensor>()
            .HasOne(s => s.Device)
            .WithMany(d => d.Sensors)
            .HasForeignKey(s => s.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<SensorLogData>()
            .HasOne(l => l.Sensor)
            .WithMany()
            .HasForeignKey(l => l.SensorId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Create indexes for better query performance
        modelBuilder.Entity<Device>()
            .HasIndex(d => d.DevEui)
            .IsUnique();
        
        modelBuilder.Entity<Sensor>()
            .HasIndex(s => new { s.DeviceId, s.Type });
        
        modelBuilder.Entity<SensorLogData>()
            .HasIndex(l => new { l.SensorId, l.Timestamp });
    }
}
