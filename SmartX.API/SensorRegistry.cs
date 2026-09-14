using SmartX.Api.Models;
using SmartX.API.Models;
namespace SmartX.API
{
    public class SensorRegistry
    {
        private readonly Dictionary<string, SensorRecord> _sensors = new();
        private readonly List<TelemetryPacket<double>> _telemetryLog = new();
        public SensorRecord Register(SensorRecord sensor)
        {
            _sensors[sensor.MacAddress] = sensor;
            return sensor;
        }
        public IEnumerable<SensorRecord> All() => _sensors.Values;

        public SensorRecord? Get(string mac) => _sensors.GetValueOrDefault(mac);

        public void RecordTelemetry(TelemetryPacket<double> packet) => _telemetryLog.Add(packet);

        public IReadOnlyList<TelemetryPacket<double>> TelemetryFor(string sensorId) =>
            _telemetryLog.Where(t => t.SensorId == sensorId).ToList();

        public bool AttachFile(string mac, string filePath)
        {
            if (!_sensors.TryGetValue(mac, out var sensor)) return false;
            sensor.AttachedFiles.Add(filePath);
            return true;
        }
    }
}
