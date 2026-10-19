namespace SmartX.Client.Models;

public enum SensorCategory { Environmental, PowerConsumption, Actuator }

/// <summary>Live condition of a device, as reported by the API (same order as the API's enum).</summary>
public enum DeviceStatus { Registered, Online, Alert, Offline }

public class SensorRecord
{
    public string MacAddress { get; set; } = string.Empty;
    public string DeploymentLocation { get; set; } = string.Empty;
    public SensorCategory Category { get; set; }
    public List<string> AttachedFiles { get; set; } = new();

    // ----- Live state, filled in by the API as telemetry arrives -----
    public DeviceStatus Status { get; set; } = DeviceStatus.Registered;
    public DateTime? LastSeen { get; set; }
    public double? LastValue { get; set; }
    public string LastUnit { get; set; } = string.Empty;
}

public class TelemetryPacket
{
    public string SensorId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;

    /// Severity is derived client-side for the dashboard's colour-coded alert feature.
    public string Severity => Math.Abs(Value) switch
    {
        > 90 => "critical",
        > 60 => "warning",
        _ => "normal"
    };
}

/// <summary>How many packets are waiting in each of the API's queues.</summary>
public class PipelineStatus
{
    public int StandardQueued { get; set; }
    public int CriticalQueued { get; set; }
}

/// <summary>One active alert from the API's HashSet (a device plus a kind of problem).</summary>
public class ActiveAlert
{
    public string DeviceId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class AlertsResponse
{
    public List<ActiveAlert> Active { get; set; } = new();
    public int SuppressedDuplicates { get; set; }
}

/// <summary>One manual override as stored on the API's command stack.</summary>
public class CommandRecord
{
    public Guid Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;          // e.g. "WaterPump"
    public string State { get; set; } = string.Empty;         // new state: "On" or "Off"
    public string PreviousState { get; set; } = string.Empty; // what Undo restores
    public DateTime IssuedAt { get; set; }
}

/// <summary>A recommendation from the prediction engine, shown as a "Suggested Action".</summary>
public class SuggestedAction
{
    public string Kind { get; set; } = string.Empty;      // "Command", "Search" or "Watch"
    public string Label { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;    // why the engine suggests it
    public double Confidence { get; set; }                // 0 to 1
    public string? DeviceId { get; set; }
    public string? CommandName { get; set; }
    public string? State { get; set; }
    public string? SearchTerm { get; set; }
}

/// <summary>A device the engine thinks needs attention, shown as an "Automated Insight".</summary>
public class Insight
{
    public string Severity { get; set; } = string.Empty;  // "Critical" or "Warning"
    public string DeviceId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class SuggestionResult
{
    public List<SuggestedAction> SuggestedActions { get; set; } = new();
    public List<Insight> Insights { get; set; } = new();
}