namespace SmartX.Api.Models;

/// <summary>
/// Demonstrates operator overloading: lets you write Meter3 = Meter1 + Meter2
/// directly in code to aggregate two smart meter readings, and compare readings
/// with &gt; / &lt; for delta/threshold checks.
/// </summary>
public class PowerReading
{
    public string MeterId { get; set; } = string.Empty;
    public double Watts { get; set; }

    public PowerReading() { }

    public PowerReading(string meterId, double watts)
    {
        MeterId = meterId;
        Watts = watts;
    }

    public static PowerReading operator +(PowerReading a, PowerReading b)
        => new($"{a.MeterId}+{b.MeterId}", a.Watts + b.Watts);

    public static PowerReading operator -(PowerReading a, PowerReading b)
        => new($"{a.MeterId}-{b.MeterId}", a.Watts - b.Watts);

    public static bool operator >(PowerReading a, PowerReading b) => a.Watts > b.Watts;
    public static bool operator <(PowerReading a, PowerReading b) => a.Watts < b.Watts;

    public override string ToString() => $"{MeterId}: {Watts:F1}W";
}