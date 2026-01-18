using Microsoft.EntityFrameworkCore;

namespace IoTGardenApi.Data;

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<IoTGardenApi.Models.Sensor> Sensors { get; set; }
        public DbSet<IoTGardenApi.Models.Device> Devices { get; set; }
        public DbSet<IoTGardenApi.Models.Alert> Alerts { get; set; }
        public DbSet<IoTGardenApi.Models.SensorLog> SensorLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Seed 7 Zones as per requirement
            // But we will use a separate seeder or controller for logic
        }
    }
