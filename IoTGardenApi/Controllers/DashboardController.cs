using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IoTGardenApi.Data;

namespace IoTGardenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var now = DateTimeOffset.UtcNow.AddHours(7);
        var oneHourAgo = now.AddHours(-1);

        // Get all sensors with latest values
        var sensors = await _context.Sensors
            .Include(s => s.Device)
            .Where(s => s.Type != "ec") // Hide EC sensor
            .Select(s => new
            {
                id = s.Id.ToString(),
                name = s.Name,
                type = s.Type,
                value = s.Value,
                unit = s.Unit,
                status = s.LastSeen > oneHourAgo ? "online" : "offline",
                lastSeen = s.LastSeen.ToString("yyyy-MM-ddTHH:mm:sszzz")
            })
            .ToListAsync();

        return Ok(new
        {
            systemStatus = "healthy",
            totalWaterUsageToday = 0,
            alerts = new string[] { },
            devices = new object[] { }, // Not used in new schema
            sensors = sensors,
            avgTemp = sensors.Where(s => s.type == "temp").Select(s => s.value).DefaultIfEmpty(0).Average(),
            avgHumid = sensors.Where(s => s.type == "humi").Select(s => s.value).DefaultIfEmpty(0).Average()
        });
    }
}
