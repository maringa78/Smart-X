using SmartX.Client.Models;

namespace SmartX.Client.Services;

/// <summary>
/// Holds the user's working state for the whole browser session (one Blazor circuit).
/// It is registered as a scoped service, so every page receives the same instance.
/// Pages read and write their state here instead of in their own fields, so nothing is
/// lost when the user switches between the three pillars and comes back.
/// Data owned by the API (sensors, alerts, history, the mesh) is not stored here; it is
/// re-fetched from the API, which is the single source of truth.
/// </summary>
public class AppState
{
    // ----- Intro Landing Modal / Presentation State -----
    public bool IntroDismissed { get; set; } = false;

    // ----- Sensor Data Ingestion and Telemetry page -----
    public SensorRecord NewSensorDraft { get; } = new();
    public List<TelemetryPacket> TelemetryFeed { get; } = new();
    public string? SensorsStatusMessage { get; set; }

    // ----- Real-Time Command Stream and History page -----
    public string SearchText { get; set; } = string.Empty;
    public SensorCategory? CategoryFilter { get; set; }
    public DeviceStatus? StatusFilter { get; set; }
    public string CommandDeviceId { get; set; } = string.Empty;
    public string CommandName { get; set; } = "WaterPump";

    // ----- Network Topology and Mesh Routing page -----
    public string? TopologyNode { get; set; }
    public bool TopologyShowBackbone { get; set; } = true;
    public string TopologyWindow { get; set; } = "120";   // minutes, or "custom"
    public DateTime? TopologyCustomFrom { get; set; }
    public DateTime? TopologyCustomTo { get; set; }
}