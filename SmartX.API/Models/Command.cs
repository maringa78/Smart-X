namespace SmartX.API.Models
{
    /// <summary>
    /// A manual override sent to a device, e.g. turn "WaterPump" on for node
    /// AA:BB:CC:DD:EE:01. PreviousState is remembered so the command can be undone.
    /// </summary>
    public class Command
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;   // e.g. "WaterPump", "Relay1"
        public string State { get; set; } = "Off";          // new state: "On" or "Off"
        public string PreviousState { get; set; } = "Off";  // state before this command
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>What the client sends when issuing a command.</summary>
    public record IssueCommandRequest(string DeviceId, string Name, string State);
}