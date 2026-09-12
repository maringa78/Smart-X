namespace SmartX.Api.Models;

/// <summary>
/// Generic wrapper for a single telemetry reading. T lets the same class carry a
/// float (soil moisture), int (power wattage), or bool (valve state) without
/// boxing/unboxing or writing a separate class per data type.
/// </summary>
public class TelemetryPacket<T>
{
    public string SensorId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public T Value { get; set; } = default!;
    public string Unit { get; set; } = string.Empty;

    public TelemetryPacket() { }

    public TelemetryPacket(string sensorId, T value, string unit)
    {
        SensorId = sensorId;
        Value = value;
        Unit = unit;
        Timestamp = DateTime.UtcNow;
    }

    public override string ToString() => $"[{Timestamp:HH:mm:ss}] {SensorId}: {Value}{Unit}";
}