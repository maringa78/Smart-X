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
builder.Services.AddSingleton<TelemetryHistory>();
builder.Services.AddSingleton<SuggestionEngine>();
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
app.MapPost("/api/commands", (IssueCommandRequest request, CommandHistory commands, SuggestionEngine suggestions) =>
{
    if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest("DeviceId and Name are required.");

    var state = request.State?.Trim();
    if (!string.Equals(state, "On", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(state, "Off", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("State must be 'On' or 'Off'.");

    var normalised = request with { State = string.Equals(state, "On", StringComparison.OrdinalIgnoreCase) ? "On" : "Off" };
    var issued = commands.Issue(normalised);
    suggestions.RecordCommand(issued);
    return Results.Ok(issued);
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

// ---- Sorted telemetry history (SortedDictionary, oldest first) ----
// Optional query string: ?from=...&to=...&limit=...
app.MapGet("/api/history/{sensorId}", (
    string sensorId,
    [Microsoft.AspNetCore.Mvc.FromQuery(Name = "from")] DateTime? start,
    [Microsoft.AspNetCore.Mvc.FromQuery(Name = "to")] DateTime? end,
    int? limit,
    TelemetryHistory history) =>
{
    var points = history.Query(sensorId, start, end, limit);
    return Results.Ok(new { sensorId, count = points.Count, points });
});

// ---- Device search and filter ----
// Add log=true when the user submits a search, so the engine can learn from it.
app.MapGet("/api/devices/search", (
    string? q,
    SensorCategory? category,
    DeviceStatus? status,
    bool? log,
    SensorRegistry registry,
    SuggestionEngine suggestions) =>
{
    if (log == true && !string.IsNullOrWhiteSpace(q))
        suggestions.RecordSearch(q);

    return Results.Ok(registry.Search(q, category, status));
});

// ---- Predictive suggestions ("Suggested Actions" and "Automated Insights") ----
app.MapGet("/api/suggestions", (SuggestionEngine suggestions, AlertTracker alerts, SensorRegistry registry) =>
    Results.Ok(suggestions.Suggest(alerts.Active(), registry.All())));

// Fills the engine with simulated operator history for demos and testing.
app.MapPost("/api/suggestions/demo-history", (SuggestionEngine suggestions) =>
{
    suggestions.SeedDemoHistory();
    return Results.Ok("Simulated operator history added.");
});

app.Run();