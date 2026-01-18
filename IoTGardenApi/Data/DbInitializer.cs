using IoTGardenApi.Models;

namespace IoTGardenApi.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        context.Database.EnsureCreated();

        // Check if already seeded
        if (context.Devices.Any())
        {
            return;
        }

        // Seed initial Device (Heltec ESP32 LoRa)
        var device = new Device
        {
            DevEui = "hetec5-1",
            Name = "Heltec ESP32 LoRa V3",
            Type = "Heltec-ESP32-LoRa-V3",
            IsOnline = false,
            LastSeen = DateTimeOffset.UtcNow
        };

        context.Devices.Add(device);
        context.SaveChanges();

        // Seed sensors for this device
        var sensors = new[]
        {
            new Sensor { DeviceId = device.Id, SlaveId = "0x01", Type = "temp", Name = "Temperature Sensor", Unit = "°C", Value = 0, Status = "offline" },
            new Sensor { DeviceId = device.Id, SlaveId = "0x01", Type = "humi", Name = "Humidity Sensor", Unit = "%", Value = 0, Status = "offline" },
            new Sensor { DeviceId = device.Id, SlaveId = "0x02", Type = "ph", Name = "PH Sensor", Unit = "pH", Value = 0, Status = "offline" },
            new Sensor { DeviceId = device.Id, SlaveId = "0x03", Type = "ec", Name = "EC Sensor", Unit = "μS/cm", Value = 0, Status = "offline" },
            new Sensor { DeviceId = device.Id, SlaveId = "0x04", Type = "n", Name = "Nitrogen Sensor", Unit = "mg/kg", Value = 0, Status = "offline" },
            new Sensor { DeviceId = device.Id, SlaveId = "0x04", Type = "p", Name = "Phosphorus Sensor", Unit = "mg/kg", Value = 0, Status = "offline" },
            new Sensor { DeviceId = device.Id, SlaveId = "0x04", Type = "k", Name = "Potassium Sensor", Unit = "mg/kg", Value = 0, Status = "offline" }
        };

        context.Sensors.AddRange(sensors);
        context.SaveChanges();
    }
}
