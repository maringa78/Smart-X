namespace SmartX.Client.Models;

public enum SensorCategory { Environmental, PowerConsumption, Actuator }

public class SensorRecord
{
    public string MacAddress { get; set; } = string.Empty;
    public string DeploymentLocation { get; set; } = string.Empty;
    public SensorCategory Category { get; set; }
    public List<string> AttachedFiles { get; set; } = new();
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
