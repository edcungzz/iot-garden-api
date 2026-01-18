using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IoTGardenApi.Data;
using IoTGardenApi.Models;
using System.Text.Json;

namespace IoTGardenApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SensorController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SensorController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Webhook for The Things Stack (LoRaWAN)
    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebHook([FromBody] JsonElement payload)
    {
        try
        {
            // 1. Extract Device ID
            if (!payload.TryGetProperty("end_device_ids", out var deviceIds) ||
                !deviceIds.TryGetProperty("device_id", out var devIdElement))
            {
                return BadRequest("Invalid Payload: Missing device_id");
            }
            string deviceId = devIdElement.GetString() ?? "unknown_device";

            // 2. Extract Decoded Payload
            if (!payload.TryGetProperty("uplink_message", out var uplink) ||
                !uplink.TryGetProperty("decoded_payload", out var decoded))
            {
                return BadRequest("Invalid Payload: Missing decoded_payload");
            }

            // 3. Process each sensor value dynamically
            var now = DateTime.UtcNow;
            var measurements = decoded.EnumerateObject();

            foreach (var measure in measurements)
            {
                string key = measure.Name; // e.g., "temp", "humi", "ph"
                
                // Safety check for numeric values
                if (measure.Value.ValueKind != JsonValueKind.Number) continue;
                double value = measure.Value.GetDouble();

                // Construct a unique Sensor ID: e.g., "heltec-v3-01_temp"
                string uniqueSensorId = $"{deviceId}_{key}";

                // A. Update or Create Real-time Sensor Status
                var sensor = await _context.Sensors.FindAsync(uniqueSensorId);
                if (sensor == null)
                {
                    sensor = new Sensor
                    {
                        Id = uniqueSensorId,
                        Name = $"{deviceId} {key.ToUpper()}",
                        Type = key,
                        Unit = GetUnit(key)
                    };
                    _context.Sensors.Add(sensor);
                }

                sensor.Value = value;
                sensor.Status = "online";
                sensor.LastSeen = now;

                // B. Historical Logging (15-Minute Rule)
                // Check the last log for this specific sensor
                var lastLog = await _context.SensorLogs
                    .Where(l => l.SensorId == uniqueSensorId)
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefaultAsync();

                if (lastLog == null || (now - lastLog.Timestamp).TotalMinutes >= 15)
                {
                    _context.SensorLogs.Add(new SensorLog
                    {
                        SensorId = uniqueSensorId,
                        Value = value,
                        Timestamp = now
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Data processed successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error processing webhook: {ex.Message}");
        }
    }

    private string GetUnit(string key)
    {
        return key.ToLower() switch
        {
            "temp" => "°C",
            "humi" => "%",
            "ph" => "pH",
            "ec" => "µS/cm",
            "n" => "mg/kg",
            "p" => "mg/kg",
            "k" => "mg/kg",
            _ => ""
        };
    }

    // GET: api/Sensor/list
    [HttpGet("list")]
    public async Task<IActionResult> GetSensorList()
    {
        try
        {
            var sensors = await _context.Sensors
                .OrderBy(s => s.Type)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return Ok(sensors);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving sensors: {ex.Message}");
        }
    }

    // GET: api/Sensor/history?sensorId=xxx&hours=24
    [HttpGet("history")]
    public async Task<IActionResult> GetSensorHistory(
        [FromQuery] string? sensorId = null,
        [FromQuery] int hours = 24)
    {
        try
        {
            // Calculate cutoff time matching the "Shifted UTC" data in DB
            // We create a DateTimeOffset with Offset 0, but using the Shifted Time value
            var nowShifted = DateTime.UtcNow.AddHours(7);
            var cutoffTime = new DateTimeOffset(nowShifted.AddHours(-hours), TimeSpan.Zero);
            

            
            // Client-side filtering fallback to bypass Npgsql Timezone Strictness
            // Fetch last 5000 records (approx 1 week of data for 7 sensors)
            // Nuclear Option: Bypass Npgsql Timestamp Mapping entirely
            // Read Timestamp as String to avoid Timezone exceptions
            string sql = "SELECT \"Id\", \"SensorId\", \"Value\", CAST(\"Timestamp\" AS text) AS \"Timestamp\" FROM \"SensorLogs\"";
            
            var dbQuery = _context.Database.SqlQueryRaw<SensorLogStringDto>(sql);

            if (!string.IsNullOrEmpty(sensorId))
            {
                // Note: appending WHERE to Raw SQL IQueryable works if EF Core composes it.
                // But specifically for SqlQueryRaw, EF Core wraps it in a subquery: SELECT ... FROM (raw) as x WHERE ...
                // This is perfect.
                dbQuery = dbQuery.Where(l => l.SensorId == sensorId);
            }

            var rawLogsString = await dbQuery
                .OrderByDescending(l => l.Timestamp) // ISO Strings sort correctly
                .Take(5000)
                .ToListAsync();

            // Convert back to strong types in memory
            var rawLogs = rawLogsString.Select(l => new 
            {
                l.Id,
                l.SensorId,
                l.Value,
                Timestamp = DateTimeOffset.Parse(l.Timestamp)
            }).ToList();

            // Filter in memory using C# logic which is robust
            // Use Ticks comparison to ignore Timezone/Offset mismatches completely
            var cutoffTicks = cutoffTime.Ticks;
            
            var logs = rawLogs
                .Where(l => l.Timestamp.Ticks >= cutoffTicks)
                .OrderBy(l => l.Timestamp)
                .Select(l => new
                {
                    l.Id,
                    l.SensorId,
                    l.Value,
                    l.Timestamp
                })
                .ToList();

            return Ok(logs);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error retrieving history: {ex.Message} Inner: {ex.InnerException?.Message}";
            Console.WriteLine(errorMsg);
            return StatusCode(500, errorMsg);
        }
    }

    // GET: api/Sensor/stats?hours=24
    [HttpGet("stats")]
    public async Task<IActionResult> GetSensorStats([FromQuery] int hours = 24)
    {
        try
        {
            // Calculate cutoff time matching the "Shifted UTC" data in DB
            var nowShifted = DateTime.UtcNow.AddHours(7);
            var cutoffTime = new DateTimeOffset(nowShifted.AddHours(-hours), TimeSpan.Zero);
            
            // Client-side filtering fallback
            // Nuclear Option: Read as String
            string sql = "SELECT \"Id\", \"SensorId\", \"Value\", CAST(\"Timestamp\" AS text) AS \"Timestamp\" FROM \"SensorLogs\"";
            
            var rawLogsString = await _context.Database.SqlQueryRaw<SensorLogStringDto>(sql)
                .OrderByDescending(l => l.Timestamp)
                .Take(5000)
                .ToListAsync();

            var rawLogs = rawLogsString.Select(l => new 
            {
                l.Id,
                l.SensorId,
                l.Value,
                Timestamp = DateTimeOffset.Parse(l.Timestamp)
            }).ToList();

             var cutoffTicks = cutoffTime.Ticks;

             var stats = rawLogs
                .Where(l => l.Timestamp.Ticks >= cutoffTicks)
                .GroupBy(l => l.SensorId)
                .Select(g => new
                {
                    SensorId = g.Key,
                    Count = g.Count(),
                    MinValue = g.Min(l => l.Value),
                    MaxValue = g.Max(l => l.Value),
                    AvgValue = g.Average(l => l.Value),
                    LatestTimestamp = g.Max(l => l.Timestamp)
                })
                .ToList();

            // Dictionary lookup for fast access and to avoid DbContext threading issues
            var sensorsIdx = await _context.Sensors.ToDictionaryAsync(s => s.Id);

            // Join with Sensors table to get metadata
            var enrichedStats = stats.Select(stat =>
            {
                sensorsIdx.TryGetValue(stat.SensorId, out var sensor);
                return new
                {
                    stat.SensorId,
                    SensorName = sensor?.Name ?? stat.SensorId,
                    SensorType = sensor?.Type ?? "unknown",
                    Unit = sensor?.Unit ?? "",
                    stat.Count,
                    stat.MinValue,
                    stat.MaxValue,
                    stat.AvgValue,
                    stat.LatestTimestamp
                };
            }).ToList();

            return Ok(enrichedStats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving stats: {ex.Message}");
        }
    }
}

public class SensorLogStringDto
{
    public int Id { get; set; }
    public string SensorId { get; set; } = "";
    public double Value { get; set; }
    public string Timestamp { get; set; } = "";
}
