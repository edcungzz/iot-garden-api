using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IoTGardenApi.Data;
using IoTGardenApi.Models;

namespace IoTGardenApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardData()
    {
        var sensors = await _context.Sensors.ToListAsync();
        var devices = await _context.Devices.ToListAsync();
        var alerts = await _context.Alerts.OrderByDescending(a => a.CreatedAt).Take(5).Select(a => a.Message).ToListAsync();

        // Calculate real averages (using actual database type names)
        var avgTemp = sensors.Where(s => s.Type == "temp").Select(s => s.Value).DefaultIfEmpty(0).Average();
        var avgHumid = sensors.Where(s => s.Type == "humi").Select(s => s.Value).DefaultIfEmpty(0).Average();

        // Real data: 0 Liters until flow sensor integration
        var totalWaterUsage = 0; 

        var systemStatus = "healthy";

        return Ok(new
        {
            sensors, // Keep full list for detailed view if needed
            devices,
            systemStatus,
            totalWaterUsageToday = totalWaterUsage,
            avgTemp,
            avgHumid,
            alerts
        });
    }

    [HttpPost("toggle/{deviceId}")]
    public async Task<IActionResult> ToggleDevice(string deviceId)
    {
        var device = await _context.Devices.FindAsync(deviceId);
        if (device == null)
        {
            return NotFound();
        }

        device.IsOn = !device.IsOn;
        device.LastActive = DateTime.UtcNow;

        // Logic: Interlock System
        if (device.Id.ToLower().Contains("pump"))
        {
             // If Pump is turned OFF, turn OFF all valves to prevent pressure buildup/invalid state
             if (!device.IsOn)
             {
                 var valves = await _context.Devices.Where(d => !d.Id.ToLower().Contains("pump") && d.IsOn).ToListAsync();
                 foreach (var valve in valves)
                 {
                     valve.IsOn = false;
                     valve.LastActive = DateTime.UtcNow;
                 }
             }
        }
        else
        {
            // If a Valve is turned ON, ensure the Pump is ON
            if (device.IsOn)
            {
                var pump = await _context.Devices.FirstOrDefaultAsync(d => d.Id.ToLower().Contains("pump"));
                if (pump != null && !pump.IsOn)
                {
                    pump.IsOn = true;
                    pump.LastActive = DateTime.UtcNow;
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok(device);
    }
}
