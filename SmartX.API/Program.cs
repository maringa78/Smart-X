using SmartX.Api;
using SmartX.Api.Models;
using SmartX.API;
using SmartX.API.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<SensorRegistry>();
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
app.MapPost("/api/telemetry", (TelemetryPacket<double> packet, SensorRegistry registry) =>
{
    registry.RecordTelemetry(packet);
    return Results.Ok(packet);
});

app.MapGet("/api/telemetry/{sensorId}", (string sensorId, SensorRegistry registry) =>
    Results.Ok(registry.TelemetryFor(sensorId)));

// --- File / log / photo upload ---
app.MapPost("/api/sensors/{mac}/upload", async (string mac, IFormFile file, SensorRegistry registry) =>
{
    Directory.CreateDirectory("uploads");
    var path = Path.Combine("uploads", $"{mac}_{file.FileName}");

    await using var stream = File.Create(path);
    await file.CopyToAsync(stream);

    var attached = registry.AttachFile(mac, path);
    return attached ? Results.Ok(new { path }) : Results.NotFound("Sensor not registered.");
})
.DisableAntiforgery();

app.Run();