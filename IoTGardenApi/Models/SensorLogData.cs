using System;

namespace IoTGardenApi.Models;

/// <summary>
/// Historical sensor readings
/// Each log entry records one measurement from a specific sensor
/// </summary>
public class SensorLogData
{
    public int Id { get; set; }
    
    /// <summary>
    /// Which sensor took this measurement
    /// </summary>
    public int SensorId { get; set; }
    
    /// <summary>
    /// Measured value
    /// </summary>
    public double Value { get; set; }
    
    /// <summary>
    /// When this measurement was taken
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Navigation property to sensor
    /// </summary>
    public Sensor Sensor { get; set; } = null!;
}
