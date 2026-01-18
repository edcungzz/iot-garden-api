using System;

namespace IoTGardenApi.Models;

public class SensorLog
{
    public int Id { get; set; }
    public string SensorId { get; set; } = string.Empty; // e.g., "heltec-v3-01-temp"
    public double Value { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
