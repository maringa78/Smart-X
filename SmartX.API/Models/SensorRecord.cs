namespace SmartX.API.Models
{
    public enum SensorCategory
    {
        Environmental,
        PowerConsumption,
        Actuator

    }

    /// <summary>Live condition of a registered device.</summary>
    public enum DeviceStatus
    {
        Registered,   // registered but has not sent any telemetry yet
        Online,       // sending normal readings
        Alert,        // latest reading was critical
        Offline       // has reported before but has gone quiet
    }

    public class SensorRecord
    {
        public string MacAddress { get; set; } = string.Empty;

        public string DeploymentLocation { get; set; } = string.Empty;

        public SensorCategory Category { get; set; }

        public List<string> AttachedFiles { get; set; } = new();

        // ---- Live state, updated by the telemetry processor ----
        public DeviceStatus Status { get; set; } = DeviceStatus.Registered;

        public DateTime? LastSeen { get; set; }

        public double? LastValue { get; set; }

        public string LastUnit { get; set; } = string.Empty;
    }
}