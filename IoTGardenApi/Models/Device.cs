namespace IoTGardenApi.Models;

public class Device
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsOn { get; set; }
    public DateTime? LastActive { get; set; }
}
