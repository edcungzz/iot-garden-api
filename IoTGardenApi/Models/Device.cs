using System;
using System.Collections.Generic;

namespace IoTGardenApi.Models;

/// <summary>
/// Main LoRa Gateway/Device (e.g., Heltec ESP32 LoRa V3)
/// One device can have multiple sensors attached
/// </summary>
public class Device
{
    public int Id { get; set; }
    
    /// <summary>
    /// TTN Device EUI (e.g., "hetec5-1")
    /// </summary>
    public string DevEui { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Device type (e.g., "Heltec-ESP32-LoRa-V3")
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;
    
    public bool IsOnline { get; set; } = false;
    
    /// <summary>
    /// Sensors attached to this device
    /// </summary>
    public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
}
