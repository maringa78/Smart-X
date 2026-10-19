using SmartX.Client.Models;

namespace SmartX.Client.Services;

/// <summary>
/// Holds the user's working state for the whole browser session (one Blazor circuit).
/// It is registered as a scoped service, so every page receives the same instance.
/// Pages read and write their state here instead of in their own fields, so nothing is
/// lost when the user switches between the three pillars and comes back.
/// Data owned by the API (sensors, alerts, history) is not stored here; it is re-fetched
/// from the API, which is the single source of truth.
/// </summary>
public class AppState
{
    // ----- Sensor Data Ingestion and Telemetry page -----
    public SensorRecord NewSensorDraft { get; } = new();
    public List<TelemetryPacket> TelemetryFeed { get; } = new();
    public string? SensorsStatusMessage { get; set; }

    // ----- Real-Time Command Stream and History page -----
    // The search box and filters live here, so they are still set when the user
    // switches to another pillar and comes back.
    public string SearchText { get; set; } = string.Empty;
    public SensorCategory? CategoryFilter { get; set; }
    public DeviceStatus? StatusFilter { get; set; }
    public string CommandDeviceId { get; set; } = string.Empty;
    public string CommandName { get; set; } = "WaterPump";
}