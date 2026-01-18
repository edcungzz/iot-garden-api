namespace IoTGardenApi.Models;

public class Sensor
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = "offline";
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}
