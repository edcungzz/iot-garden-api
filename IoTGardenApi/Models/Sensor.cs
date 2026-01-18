using System;

namespace IoTGardenApi.Models;

/// <summary>
/// Physical sensor attached to a Device
/// Each sensor has a unique SlaveId (address) within the device
/// </summary>
public class Sensor
{
    public int Id { get; set; }
    
    /// <summary>
    /// Which device this sensor is connected to
    /// </summary>
    public int DeviceId { get; set; }
    
    /// <summary>
    /// Sensor address/slave ID (e.g., 0x01, 0x02)
    /// Used for Modbus/RS485 communication
    /// </summary>
    public string SlaveId { get; set; } = string.Empty;
    
    /// <summary>
    /// Sensor type: temp, humi, ph, ec, n, p, k
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public string Unit { get; set; } = string.Empty;
    
    /// <summary>
    /// Current value (latest reading)
    /// </summary>
    public double Value { get; set; }
    
    public string Status { get; set; } = "offline"; // "online" or "offline"
    
    public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Navigation property to parent device
    /// </summary>
    public Device Device { get; set; } = null!;
}
