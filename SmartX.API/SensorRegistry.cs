using SmartX.Api.Models;
using SmartX.API.Models;
using SmartX.API.Services;
namespace SmartX.API
{
    /// <summary>
    /// Store of all registered devices. The Dictionary is keyed by MAC address, so
    /// finding the device that a new telemetry packet belongs to is an O(1) lookup
    /// instead of scanning a list.
    /// </summary>
    public class SensorRegistry
    {
        private readonly Dictionary<string, SensorRecord> _sensors = new();
        private readonly List<TelemetryPacket<double>> _telemetryLog = new();

        // The background worker writes while API requests read, so access is locked.
        private readonly object _lock = new();

        public SensorRecord Register(SensorRecord sensor)
        {
            lock (_lock)
            {
                // Re-registering an existing MAC updates its details but keeps its live state.
                if (_sensors.TryGetValue(sensor.MacAddress, out var existing))
                {
                    existing.DeploymentLocation = sensor.DeploymentLocation;
                    existing.Category = sensor.Category;
                    return existing;
                }

                _sensors[sensor.MacAddress] = sensor;
                return sensor;
            }
        }

        public List<SensorRecord> All()
        {
            lock (_lock) return _sensors.Values.ToList();
        }

        public SensorRecord? Get(string mac)
        {
            lock (_lock) return _sensors.GetValueOrDefault(mac);
        }

        /// <summary>
        /// Stores the packet and updates the owning device's live state.
        /// Returns false if the packet came from a device that is not registered.
        /// </summary>
        public bool RecordTelemetry(TelemetryPacket<double> packet)
        {
            lock (_lock)
            {
                _telemetryLog.Add(packet);

                // O(1) lookup by MAC address
                if (!_sensors.TryGetValue(packet.SensorId, out var device))
                    return false;

                device.LastSeen = packet.Timestamp;
                device.LastValue = packet.Value;
                device.LastUnit = packet.Unit;
                device.Status = TelemetryPipeline.TryGetCriticalPriority(packet, out _)
                    ? DeviceStatus.Alert
                    : DeviceStatus.Online;
                return true;
            }
        }

        public IReadOnlyList<TelemetryPacket<double>> TelemetryFor(string sensorId)
        {
            lock (_lock)
                return _telemetryLog.Where(t => t.SensorId == sensorId).ToList();
        }

        /// <summary>Finds devices by text (MAC or location), category and/or live status.</summary>
        public List<SensorRecord> Search(string? query, SensorCategory? category, DeviceStatus? status)
        {
            lock (_lock)
                return _sensors.Values
                    .Where(s => string.IsNullOrWhiteSpace(query)
                                || s.MacAddress.Contains(query, StringComparison.OrdinalIgnoreCase)
                                || s.DeploymentLocation.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Where(s => category is null || s.Category == category)
                    .Where(s => status is null || s.Status == status)
                    .ToList();
        }

        /// <summary>MAC addresses of devices that have reported before but not within the timeout.</summary>
        public List<string> GetStaleDevices(TimeSpan timeout)
        {
            var cutoff = DateTime.UtcNow - timeout;
            lock (_lock)
                return _sensors.Values
                    .Where(s => s.LastSeen is not null && s.LastSeen < cutoff)
                    .Select(s => s.MacAddress)
                    .ToList();
        }

        public void SetStatus(string mac, DeviceStatus status)
        {
            lock (_lock)
            {
                if (_sensors.TryGetValue(mac, out var sensor))
                    sensor.Status = status;
            }
        }

        public bool AttachFile(string mac, string filePath)
        {
            lock (_lock)
            {
                if (!_sensors.TryGetValue(mac, out var sensor)) return false;
                sensor.AttachedFiles.Add(filePath);
                return true;
            }
        }
    }
}