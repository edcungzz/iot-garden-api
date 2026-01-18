using IoTGardenApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IoTGardenApi.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        // Ensure database is created
        context.Database.EnsureCreated();
        
        // Safety fallback: Ensure SensorLogs table exists (if migrations didn't run)
        try 
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""SensorLogs"" (
                    ""Id"" serial NOT NULL,
                    ""SensorId"" text NOT NULL,
                    ""Value"" double precision NOT NULL,
                    ""Timestamp"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_SensorLogs"" PRIMARY KEY (""Id"")
                );
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Table creation warning: {ex.Message}");
        }

        // Check if DB has been seeded
        if (context.Devices.Any())
        {
            return;   // DB has been seeded
        }

        var devices = new Device[]
        {
            new Device { Id = "valve1", Name = "Zone 1 (Flower Bed)", IsOn = true },
            new Device { Id = "valve2", Name = "Zone 2 (Vegetables)", IsOn = false },
            new Device { Id = "valve3", Name = "Zone 3 (Orchard)", IsOn = false },
            new Device { Id = "valve4", Name = "Zone 4 (Front)", IsOn = false },
            new Device { Id = "valve5", Name = "Zone 5 (Side)", IsOn = true },
            new Device { Id = "valve6", Name = "Zone 6 (Back)", IsOn = false },
            new Device { Id = "valve7", Name = "Zone 7 (Deck)", IsOn = false },
            new Device { Id = "pump1", Name = "Main Pump", IsOn = true }
        };

        context.Devices.AddRange(devices);

        var sensors = new Sensor[]
        {
            new Sensor { Id = "s1", Name = "Main Weather Station", Type = "temp", Value = 28.5, Unit = "°C", Status = "online", LastSeen = DateTime.UtcNow },
            new Sensor { Id = "s2", Name = "Ambient Humidity", Type = "humidity", Value = 65, Unit = "%", Status = "online", LastSeen = DateTime.UtcNow },
            new Sensor { Id = "s3", Name = "Soil Moisture Z1", Type = "soil", Value = 42, Unit = "%", Status = "online", LastSeen = DateTime.UtcNow.AddMinutes(-2) }
        };

        context.Sensors.AddRange(sensors);

        var alerts = new Alert[]
        {
            new Alert { Message = "System boot successful at 06:00 AM", CreatedAt = DateTime.UtcNow.Date.AddHours(6) },
            new Alert { Message = "Zone 2 scheduled watering completed", CreatedAt = DateTime.UtcNow.AddHours(-1) }
        };

        context.Alerts.AddRange(alerts);

        context.SaveChanges();
    }
}
