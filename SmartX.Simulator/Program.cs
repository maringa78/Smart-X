using System.Net.Http.Json;
using System.Text.Json;

// Smart-X device simulator.
// Acts like a fleet of ESP32 devices: it registers them with the API, then keeps
// sending telemetry over HTTP exactly as real hardware would.
// Usage: dotnet run -- [apiBaseUrl]     (default https://localhost:7008)

var baseUrl = args.Length > 0 ? args[0] : "https://localhost:7008";

var handler = new HttpClientHandler
{
    // Development only: trust the local ASP.NET dev certificate for localhost.
    ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
        errors == System.Net.Security.SslPolicyErrors.None || request.RequestUri?.IsLoopback == true
};
using var http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };

var rnd = new Random();

// Category numbers match the API: 0 = Environmental, 1 = PowerConsumption, 2 = Activator
var devices = new List<Device>
{
    new("AA:BB:CC:DD:EE:11", "Hydroponics Farm - Zone 1",   0, "%", 40, 70, 55),
    new("AA:BB:CC:DD:EE:12", "Hydroponics Farm - Zone 2",   0, "%", 40, 70, 60),
    new("AA:BB:CC:DD:EE:13", "Hydroponics Farm - Zone 3",   0, "%", 40, 70, 50),
    new("AA:BB:CC:DD:EE:21", "Smart Grid - Substation A",   1, "W", 800, 2200, 1500),
    new("AA:BB:CC:DD:EE:22", "Smart Grid - Substation B",   1, "W", 800, 2200, 1200),
    new("AA:BB:CC:DD:EE:23", "Office Block - Floor 1 Meter", 1, "W", 500, 1800, 900),
    new("AA:BB:CC:DD:EE:31", "Greenhouse - Room 1",         0, "C", 18, 30, 24),
    new("AA:BB:CC:DD:EE:32", "Greenhouse - Room 2",         0, "C", 18, 30, 22),
};

int sent = 0;
int failed = 0;

// ---------- helpers ----------

async Task SendAsync(Device d, double value)
{
    try
    {
        var response = await http.PostAsJsonAsync("/api/telemetry",
            new { sensorId = d.Mac, timestamp = DateTime.UtcNow, value, unit = d.Unit });

        if (response.IsSuccessStatusCode) Interlocked.Increment(ref sent);
        else Interlocked.Increment(ref failed);
    }
    catch (HttpRequestException)
    {
        Interlocked.Increment(ref failed);
    }
}

async Task<(int Standard, int Critical)?> QueueStatusAsync()
{
    try
    {
        var json = await http.GetFromJsonAsync<JsonElement>("/api/pipeline/status");
        return (json.GetProperty("standardQueued").GetInt32(),
                json.GetProperty("criticalQueued").GetInt32());
    }
    catch
    {
        return null;
    }
}

// Sends a large burst at once so the queues visibly fill up.
// Every 10th packet is a critical one, so you can watch them jump the queue.
async Task BurstAsync(int count)
{
    var online = devices.Where(d => d.Connected).ToList();
    if (online.Count == 0)
    {
        Console.WriteLine("  No devices are connected, so nothing can send a burst.");
        return;
    }

    var tasks = new List<Task>();
    for (int i = 0; i < count; i++)
    {
        var d = online[rnd.Next(online.Count)];
        var critical = i % 10 == 0 ? d.CriticalValue(rnd) : null;
        tasks.Add(SendAsync(d, critical ?? d.NextValue(rnd)));
    }
    await Task.WhenAll(tasks);

    Console.WriteLine($"  Burst of {count} packets sent. Watching the queues drain...");
    for (int i = 0; i < 25; i++)
    {
        var status = await QueueStatusAsync();
        if (status is null) break;

        Console.WriteLine($"    queued: standard={status.Value.Standard}, critical={status.Value.Critical}");
        if (status.Value.Standard == 0 && status.Value.Critical == 0) break;
        await Task.Delay(300);
    }
}

Device? PickOnline(string unit) =>
    devices.Where(d => d.Connected && d.Unit == unit).OrderBy(_ => rnd.Next()).FirstOrDefault();

// ---------- register devices ----------

Console.WriteLine($"Smart-X simulator targeting {baseUrl}");
Console.WriteLine("Registering devices...");
foreach (var d in devices)
{
    try
    {
        var response = await http.PostAsJsonAsync("/api/sensors",
            new { macAddress = d.Mac, deploymentLocation = d.Location, category = d.Category });
        Console.WriteLine($"  {d.Mac}  {d.Location}  ({(int)response.StatusCode})");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Cannot reach the API at {baseUrl}: {ex.Message}");
        Console.WriteLine("Start SmartX.API first, then run the simulator again.");
        return;
    }
}

Console.WriteLine();
Console.WriteLine("Sending a reading from every connected device every 2 seconds.");
Console.WriteLine("Keys:");
Console.WriteLine("  S = power spike on a random meter     M = moisture drop on a random sensor");
Console.WriteLine("  D = disconnect a random device        R = reconnect all devices");
Console.WriteLine("  B = burst of 300 packets              Q = quit");
Console.WriteLine();

// ---------- main loop ----------

while (true)
{
    while (Console.KeyAvailable)
    {
        switch (Console.ReadKey(true).Key)
        {
            case ConsoleKey.S:
                var meter = PickOnline("W");
                if (meter is not null)
                {
                    meter.CriticalReadingsLeft = 3;
                    Console.WriteLine($"  >> Power spike on {meter.Mac} ({meter.Location}) for the next 3 readings");
                }
                break;

            case ConsoleKey.M:
                var moisture = PickOnline("%");
                if (moisture is not null)
                {
                    moisture.CriticalReadingsLeft = 3;
                    Console.WriteLine($"  >> Moisture drop on {moisture.Mac} ({moisture.Location}) for the next 3 readings");
                }
                break;

            case ConsoleKey.D:
                var victim = devices.Where(d => d.Connected).OrderBy(_ => rnd.Next()).FirstOrDefault();
                if (victim is not null)
                {
                    victim.Connected = false;
                    Console.WriteLine($"  >> {victim.Mac} ({victim.Location}) went silent");
                }
                break;

            case ConsoleKey.R:
                foreach (var d in devices) d.Connected = true;
                Console.WriteLine("  >> All devices reconnected");
                break;

            case ConsoleKey.B:
                await BurstAsync(300);
                break;

            case ConsoleKey.Q:
                Console.WriteLine("Simulator stopped.");
                return;
        }
    }

    var online = devices.Where(d => d.Connected).ToList();
    await Task.WhenAll(online.Select(d => SendAsync(d, d.NextValue(rnd))));

    var queue = await QueueStatusAsync();
    var queueText = queue is null
        ? "queue status unavailable"
        : $"queued std={queue.Value.Standard} crit={queue.Value.Critical}";

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] sent={sent} failed={failed} online={online.Count}/{devices.Count}  {queueText}");

    await Task.Delay(2000);
}

// ---------- simulated device ----------

class Device(string mac, string location, int category, string unit, double min, double max, double start)
{
    public string Mac { get; } = mac;
    public string Location { get; } = location;
    public int Category { get; } = category;
    public string Unit { get; } = unit;

    public double Value { get; private set; } = start;
    public bool Connected { get; set; } = true;

    // While above zero, the device sends critical readings instead of normal ones.
    public int CriticalReadingsLeft { get; set; }

    /// <summary>A small random step from the last reading, kept inside the device's normal range.</summary>
    public double NextValue(Random rnd)
    {
        if (CriticalReadingsLeft > 0)
        {
            CriticalReadingsLeft--;
            return CriticalValue(rnd) ?? Value;
        }

        var step = (max - min) * 0.03;
        Value = Math.Clamp(Value + (rnd.NextDouble() * 2 - 1) * step, min, max);
        return Math.Round(Value, 1);
    }

    /// <summary>A value the API treats as critical (above 3000 W, or below 20 %). Null for other sensors.</summary>
    public double? CriticalValue(Random rnd) => Unit switch
    {
        "W" => Math.Round(3200 + rnd.NextDouble() * 1500, 1),
        "%" => Math.Round(5 + rnd.NextDouble() * 13, 1),
        _ => null
    };
}