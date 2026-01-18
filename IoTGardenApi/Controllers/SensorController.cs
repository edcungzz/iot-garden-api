using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IoTGardenApi.Data;
using IoTGardenApi.Models;

namespace IoTGardenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SensorController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Sensor/history?hours=24
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int hours = 24)
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddHours(-hours);
        
        var logs = await _context.SensorLogData
            .Include(l => l.Sensor)
            .Where(l => l.Timestamp >= cutoffTime)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => new
            {
                id = l.Id,
                sensorId = $"{l.Sensor.Device.DevEui}_{l.Sensor.Type}",
                deviceId = l.Sensor.Device.DevEui,
                sensorType = l.Sensor.Type,
                value = l.Value,
                timestamp = l.Timestamp
            })
            .ToListAsync();

        return Ok(logs);
    }

    // GET: api/Sensor/latest
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest()
    {
        var sensors = await _context.Sensors
            .Include(s => s.Device)
            .ToListAsync();

        var result = sensors.Select(s => new
        {
            id = s.Id,
            deviceId = s.Device.DevEui,
            sensorType = s.Type,
            name = s.Name,
            value = s.Value,
            unit = s.Unit,
            status = s.Status,
            lastSeen = s.LastSeen
        }).ToList();

        return Ok(result);
    }
}
