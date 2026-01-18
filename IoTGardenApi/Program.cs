using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Supabase;
using IoTGardenApi.Data;
using IoTGardenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers()
    .AddNewtonsoftJson(); // Add support for Newtonsoft.Json

builder.Services.AddOpenApi();

// ==========================================
// ตั้งค่า Supabase
// ==========================================
var supabaseUrl = "https://fjljkiwiobhbazzqusie.supabase.co";
var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZqbGpraXdpb2JoYmF6enF1c2llIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjY4MjIxMjcsImV4cCI6MjA4MjM5ODEyN30.7uoN-hInGd2pSWO9_XMlXuix6q0oyGvqrxnz9A00gh8";

var options = new Supabase.SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true
};

var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);
await supabaseClient.InitializeAsync();

builder.Services.AddSingleton(supabaseClient);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// ==========================================
// TTN Webhook Endpoint
// ==========================================
app.MapPost("/api/uplink", async (
    HttpContext context,
    [FromServices] ApplicationDbContext dbContext,
    [FromServices] Supabase.Client supabase) =>
{
    try
    {
        // Read raw body as string then parse to JObject
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var payload = JObject.Parse(body);
        
        Console.WriteLine("=== Received Webhook ===");
        Console.WriteLine($"Raw payload: {payload}");
        
        // ดึง device_id จาก payload
        var deviceId = payload["end_device_ids"]?["device_id"]?.ToString() ?? "unknown";
        Console.WriteLine($"Device ID: {deviceId}");
        
        // ดึงค่า sensor จาก payload
        var decodedPayload = payload["uplink_message"]?["decoded_payload"];
        Console.WriteLine($"Decoded payload: {decodedPayload}");
        
        if (decodedPayload == null)
        {
            Console.WriteLine("❌ No decoded_payload found!");
            return Results.BadRequest("No decoded_payload found");
        }

        var temp = decodedPayload["temp"]?.ToObject<float>() ?? 0;
        var humi = decodedPayload["humi"]?.ToObject<float>() ?? 0;
        var ec = decodedPayload["ec"]?.ToObject<int>() ?? 0;
        var ph = decodedPayload["ph"]?.ToObject<float>() ?? 0;
        var n = decodedPayload["n"]?.ToObject<int>() ?? 0;
        var p = decodedPayload["p"]?.ToObject<int>() ?? 0;
        var k = decodedPayload["k"]?.ToObject<int>() ?? 0;

        Console.WriteLine($"Temp={temp}°C, Humi={humi}%, PH={ph}, N={n}, P={p}, K={k}");

        // 1. บันทึกลง Supabase
        try
        {
            var supabaseLog = new SupabaseSensorLog
            {
                device_id = deviceId,
                temp = temp,
                humi = humi,
                ec = ec,
                ph = ph,
                n = n,
                p = p,
                k = k,
                created_at = DateTime.UtcNow.AddHours(7)
            };
            
            await supabase.From<SupabaseSensorLog>().Insert(supabaseLog);
            Console.WriteLine($"✅ Saved to Supabase");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Supabase Error: {ex.Message}");
        }

        // 2. บันทึกลง Local Database
        var now = DateTimeOffset.UtcNow.AddHours(7);
        
        // Find or create device
        var device = await dbContext.Devices
            .Include(d => d.Sensors)
            .FirstOrDefaultAsync(d => d.DevEui == deviceId);
        
        if (device == null)
        {
            device = new Device
            {
                DevEui = deviceId,
                Name = $"Device {deviceId}",
                Type = "LoRa Gateway",
                IsOnline = true,
                LastSeen = now
            };
            dbContext.Devices.Add(device);
            await dbContext.SaveChangesAsync();
            
            // Create default sensors
            var sensorTypes = new[] { "temp", "humi", "ph", "ec", "n", "p", "k" };
            foreach (var type in sensorTypes)
            {
                dbContext.Sensors.Add(new Sensor
                {
                    DeviceId = device.Id,
                    SlaveId = "0x01",
                    Type = type,
                    Name = $"{type.ToUpper()} Sensor",
                    Unit = type == "temp" ? "°C" : type == "humi" ? "%" : type == "ph" ? "pH" : type == "ec" ? "μS/cm" : "mg/kg",
                    Value = 0,
                    Status = "offline"
                });
            }
            await dbContext.SaveChangesAsync();
        }
        else
        {
            device.IsOnline = true;
            device.LastSeen = now;
        }

        // Update sensors and log data
        var sensorData = new Dictionary<string, double>
        {
            ["temp"] = temp,
            ["humi"] = humi,
            ["ph"] = ph,
            ["ec"] = ec,
            ["n"] = n,
            ["p"] = p,
            ["k"] = k
        };

        foreach (var kvp in sensorData)
        {
            var sensor = device.Sensors.FirstOrDefault(s => s.Type == kvp.Key);
            if (sensor != null)
            {
                sensor.Value = kvp.Value;
                sensor.Status = "online";
                sensor.LastSeen = now;
                
                // Log every 15 minutes
                var lastLog = await dbContext.SensorLogData
                    .Where(l => l.SensorId == sensor.Id)
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefaultAsync();
                
                if (lastLog == null || (now - lastLog.Timestamp).TotalMinutes >= 15)
                {
                    dbContext.SensorLogData.Add(new SensorLogData
                    {
                        SensorId = sensor.Id,
                        Value = kvp.Value,
                        Timestamp = now
                    });
                }
            }
        }

        await dbContext.SaveChangesAsync();
        Console.WriteLine("✅ Saved to Local Database");

        return Results.Ok(new { message = "Data received and saved successfully" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
        return Results.Problem(ex.Message);
    }
});

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    DbInitializer.Initialize(context);
}

app.Run();

// ==========================================
// Supabase Model
// ==========================================
[Supabase.Postgrest.Attributes.Table("sensor_logs")]
public class SupabaseSensorLog : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.Column("device_id")]
    public string? device_id { get; set; }

    [Supabase.Postgrest.Attributes.Column("temp")]
    public float temp { get; set; }

    [Supabase.Postgrest.Attributes.Column("humi")]
    public float humi { get; set; }

    [Supabase.Postgrest.Attributes.Column("ec")]
    public int ec { get; set; }

    [Supabase.Postgrest.Attributes.Column("ph")]
    public float ph { get; set; }

    [Supabase.Postgrest.Attributes.Column("n")]
    public int n { get; set; }

    [Supabase.Postgrest.Attributes.Column("p")]
    public int p { get; set; }

    [Supabase.Postgrest.Attributes.Column("k")]
    public int k { get; set; }

    [Supabase.Postgrest.Attributes.Column("created_at")]
    public DateTime created_at { get; set; }
}