using SmartX.Api.Models;

namespace SmartX.API.Services
{
    /// <summary>
    /// Historical readings for every sensor, kept in time order.
    /// Each sensor gets its own SortedDictionary keyed by timestamp, so the
    /// readings are always sorted and the dashboard can draw a timeline directly
    /// without sorting anything first. A normal Dictionary finds the right
    /// sensor's timeline in O(1).
    /// </summary>
    public class TelemetryHistory
    {
        // Oldest readings are dropped beyond this, so memory stays bounded.
        private const int MaxPointsPerSensor = 5000;

        private readonly Dictionary<string, SortedDictionary<DateTime, TelemetryPacket<double>>> _timelines = new();
        private readonly object _lock = new();

        public void Add(TelemetryPacket<double> packet)
        {
            var key = packet.Timestamp.ToUniversalTime();

            lock (_lock)
            {
                if (!_timelines.TryGetValue(packet.SensorId, out var timeline))
                {
                    timeline = new SortedDictionary<DateTime, TelemetryPacket<double>>();
                    _timelines[packet.SensorId] = timeline;
                }

                // Keys must be unique, so two readings with the same timestamp are nudged apart.
                while (timeline.ContainsKey(key))
                    key = key.AddTicks(1);

                timeline[key] = packet;

                if (timeline.Count > MaxPointsPerSensor)
                    timeline.Remove(timeline.Keys.First());
            }
        }

        /// <summary>
        /// Readings for one sensor, oldest first. Optionally limited to a time range,
        /// and optionally to the most recent N readings.
        /// </summary>
        public List<TelemetryPacket<double>> Query(string sensorId, DateTime? from, DateTime? to, int? limit)
        {
            lock (_lock)
            {
                if (!_timelines.TryGetValue(sensorId, out var timeline))
                    return new List<TelemetryPacket<double>>();

                IEnumerable<KeyValuePair<DateTime, TelemetryPacket<double>>> points = timeline;

                if (from is not null)
                {
                    var start = from.Value.ToUniversalTime();
                    points = points.Where(p => p.Key >= start);
                }

                if (to is not null)
                {
                    var end = to.Value.ToUniversalTime();
                    points = points.Where(p => p.Key <= end);
                }

                var result = points.Select(p => p.Value);

                if (limit is > 0)
                    result = result.TakeLast(limit.Value);

                return result.ToList();
            }
        }
    }
}