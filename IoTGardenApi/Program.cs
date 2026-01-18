using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Supabase;
using IoTGardenApi.Data;
using IoTGardenApi.Models;

// Enable legacy timestamp behavior removed as we use DateTimeOffset now - STRICT MODE

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ==========================================
// ตั้งค่า Supabase (Using hardcoded credentials from previous version)
// ==========================================
var supabaseUrl = "https://fjljkiwiobhbazzqusie.supabase.co";
var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZqbGpraXdpb2JoYmF6enF1c2llIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjY4MjIxMjcsImV4cCI6MjA4MjM5ODEyN30.7uoN-hInGd2pSWO9_XMlXuix6q0oyGvqrxnz9A00gh8";

var options = new Supabase.SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true
};

// สร้าง Client
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

// Configure port for Render (read from environment variable)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5021";
app.Urls.Add($"http://0.0.0.0:{port}");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Endpoint รับค่าจาก LoRaWAN Server (The Things Stack)
app.MapPost("/api/uplink", async (HttpContext context, [FromServices] Supabase.Client supabaseClient, [FromServices] ApplicationDbContext dbContext) =>
{
    try
    {
        // อ่าน raw body เพื่อ debug
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
        
        Console.WriteLine("=== Received Webhook ===");
        Console.WriteLine($"Raw Payload: {rawBody}");
        
        // Parse JSON
        var payload = JObject.Parse(rawBody);
        
        // ดึง decoded_payload (รองรับทั้ง TTN และ ChirpStack)
        var decoded = payload["uplink_message"]?["decoded_payload"] 
                      ?? payload["object"];

        if (decoded == null)
        {
            Console.WriteLine("❌ Error: No decoded_payload found");
            Console.WriteLine($"Available keys: {string.Join(", ", payload.Properties().Select(p => p.Name))}");
            return Results.BadRequest(new { error = "No decoded payload found", received_keys = payload.Properties().Select(p => p.Name) });
        }

        Console.WriteLine($"Decoded Payload: {decoded}");

        // Extract values with null safety
        var temp = (float?)decoded["temp"] ?? (float?)decoded["temperature"] ?? 0f;
        var humi = (float?)decoded["humi"] ?? (float?)decoded["humidity"] ?? 0f;
        var ec = (int?)decoded["ec"] ?? 0;
        var ph = (float?)decoded["ph"] ?? 0f;
        var n = (int?)decoded["n"] ?? 0;
        var p = (int?)decoded["p"] ?? 0;
        var k = (int?)decoded["k"] ?? 0;

        // 1. บันทึกลง Supabase
        try
        {
            var supabaseLog = new SupabaseSensorLog
            {
                temp = temp,
                humi = humi,
                ec = ec,
                ph = ph,
                n = n,
                p = p,
                k = k,
                created_at = DateTime.UtcNow.AddHours(7) // Thailand timezone
            };
            
            await supabaseClient.From<SupabaseSensorLog>().Insert(supabaseLog);
            Console.WriteLine($"✅ Saved to Supabase: Temp={temp}°C, Humi={humi}%");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Supabase Error: {ex.Message}");
        }

        // 2. บันทึกลง Local Database (ตาม SensorController logic)
        try
        {
            var deviceId = payload["end_device_ids"]?["device_id"]?.ToString() ?? "unknown_device";
            // Use Thailand timezone (UTC+7)
            var now = DateTime.UtcNow.AddHours(7);
            
            // สร้าง/อัพเดท Sensor entries
            var sensorData = new Dictionary<string, (double value, string unit)>
            {
                { "temp", (temp, "°C") },
                { "humi", (humi, "%") },
                { "ec", (ec, "µS/cm") },
                { "ph", (ph, "pH") },
                { "n", (n, "mg/kg") },
                { "p", (p, "mg/kg") },
                { "k", (k, "mg/kg") }
            };

            foreach (var (key, (value, unit)) in sensorData)
            {
                // Don't skip zero values - display all sensors
                
                var sensorId = $"{deviceId}_{key}";
                var sensor = await dbContext.Sensors.FindAsync(sensorId);
                
                if (sensor == null)
                {
                    sensor = new Sensor
                    {
                        Id = sensorId,
                        Name = $"{deviceId} {key.ToUpper()}",
                        Type = key,
                        Unit = unit
                    };
                    dbContext.Sensors.Add(sensor);
                }
                
                sensor.Value = value;
                sensor.Status = "online";
                sensor.LastSeen = now;
                
                // Log ทุก 15 นาที
                var lastLog = await dbContext.SensorLogs
                    .Where(l => l.SensorId == sensorId)
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefaultAsync();
                
                if (lastLog == null || (now - lastLog.Timestamp).TotalMinutes >= 15)
                {
                    dbContext.SensorLogs.Add(new SensorLog
                    {
                        SensorId = sensorId,
                        Value = value,
                        Timestamp = now
                    });
                }
            }
            
            await dbContext.SaveChangesAsync();
            Console.WriteLine($"✅ Saved to Local DB: Device={deviceId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Local DB Error: {ex.Message}");
        }

        return Results.Ok(new { status = "success", temp, humi, ec, ph, n, p, k });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Fatal Error: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        return Results.Problem(ex.Message);
    }
});

app.Run();

// Model Class (Renamed to avoid conflict)
[Supabase.Postgrest.Attributes.Table("sensor_logs")]
public class SupabaseSensorLog : Supabase.Postgrest.Models.BaseModel
{
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