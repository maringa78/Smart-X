using SmartX.Api;
using SmartX.Api.Models;
using SmartX.API;
using SmartX.API.Models;
using SmartX.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<SensorRegistry>();
builder.Services.AddSingleton<TelemetryPipeline>();
builder.Services.AddSingleton<CommandHistory>();
builder.Services.AddSingleton<AlertTracker>();
builder.Services.AddHostedService<DeviceMonitor>();
builder.Services.AddHostedService<TelemetryProcessor>();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- Sensor registration ---
app.MapPost("/api/sensors", (SensorRecord sensor, SensorRegistry registry) =>
{
    registry.Register(sensor);
    return Results.Created($"/api/sensors/{sensor.MacAddress}", sensor);
});

app.MapGet("/api/sensors", (SensorRegistry registry) => Results.Ok(registry.All()));

app.MapGet("/api/sensors/{mac}", (string mac, SensorRegistry registry) =>
{
    var sensor = registry.Get(mac);
    return sensor is null ? Results.NotFound() : Results.Ok(sensor);
});

// --- Telemetry ingestion ---
app.MapPost("/api/telemetry", (TelemetryPacket<double> packet, TelemetryPipeline pipeline) =>
{
    var wasCritical = pipeline.Enqueue(packet);
    return Results.Accepted($"/api/telemetry/{packet.SensorId}", new { packet, wasCritical });
});

// Shows how many packets are waiting in each queue (useful for testing and the demo)
app.MapGet("/api/pipeline/status", (TelemetryPipeline pipeline) =>
{
    var (standard, critical) = pipeline.Counts();
    return Results.Ok(new { standardQueued = standard, criticalQueued = critical });
});

app.MapGet("/api/telemetry/{sensorId}", (string sensorId, SensorRegistry registry) =>
    Results.Ok(registry.TelemetryFor(sensorId)));

// --- File / log / photo upload ---
app.MapPost("/api/sensors/{mac}/upload", async (string mac, IFormFile file, SensorRegistry registry) =>
{
    Directory.CreateDirectory("uploads");
    var safeMac = mac.Replace(":", "-");
    var path = Path.Combine("uploads", $"{safeMac}_{file.FileName}");

    await using var stream = File.Create(path);
    await file.CopyToAsync(stream);

    var attached = registry.AttachFile(mac, path);
    return attached ? Results.Ok(new { path }) : Results.NotFound("Sensor not registered.");
})
.DisableAntiforgery();

// ---- Manual override commands (Stack-based history with undo) ----
app.MapPost("/api/commands", (IssueCommandRequest request, CommandHistory commands) =>
{
    if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest("DeviceId and Name are required.");

    var state = request.State?.Trim();
    if (!string.Equals(state, "On", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(state, "Off", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("State must be 'On' or 'Off'.");

    var normalised = request with { State = string.Equals(state, "On", StringComparison.OrdinalIgnoreCase) ? "On" : "Off" };
    return Results.Ok(commands.Issue(normalised));
});

app.MapPost("/api/commands/undo", (CommandHistory commands) =>
{
    var undone = commands.Undo();
    return undone is null
        ? Results.NotFound("No commands to undo.")
        : Results.Ok(undone);
});

app.MapGet("/api/commands/history", (CommandHistory commands) => Results.Ok(commands.History()));

// ---- Active alerts (HashSet-based, no duplicates) ----
app.MapGet("/api/alerts", (AlertTracker alerts) => Results.Ok(new
{
    active = alerts.Active().Select(a => new { a.DeviceId, type = a.Type.ToString() }),
    suppressedDuplicates = alerts.SuppressedDuplicates
}));

app.Run();