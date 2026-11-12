using SmartX.Api.Models;

namespace SmartX.API.Services
{
    public record ArchiveResult(
        List<TelemetryPacket<double>> Readings,
        int TotalReadings,
        int NodesVisited,
        int TreeHeight);

    /// <summary>
    /// Long-term, timestamped telemetry for every sensor, indexed by an AVL tree per sensor.
    /// A Dictionary finds the sensor's tree in O(1); the tree then answers time-range
    /// questions such as "all readings for Node A between 14:00 and 16:00" without
    /// scanning the sensor's whole history.
    /// </summary>
    public class TelemetryArchive
    {
        private readonly Dictionary<string, AvlTree<DateTime, TelemetryPacket<double>>> _trees = new();
        private readonly object _lock = new();

        public void Add(TelemetryPacket<double> packet)
        {
            lock (_lock)
            {
                if (!_trees.TryGetValue(packet.SensorId, out var tree))
                {
                    tree = new AvlTree<DateTime, TelemetryPacket<double>>();
                    _trees[packet.SensorId] = tree;
                }

                tree.Insert(packet.Timestamp.ToUniversalTime(), packet);
            }
        }

        /// <summary>Readings for one sensor within a time range (open-ended if from or to is null).</summary>
        public ArchiveResult Query(string sensorId, DateTime? from, DateTime? to)
        {
            lock (_lock)
            {
                if (!_trees.TryGetValue(sensorId, out var tree))
                    return new ArchiveResult(new List<TelemetryPacket<double>>(), 0, 0, 0);

                var start = from?.ToUniversalTime() ?? DateTime.MinValue;
                var end = to?.ToUniversalTime() ?? DateTime.MaxValue;

                var readings = tree.RangeQuery(start, end, out var visited);
                return new ArchiveResult(readings, tree.Count, visited, tree.Height);
            }
        }

        /// <summary>
        /// Demo helper: bulk-loads simulated history (one reading per minute, ending now)
        /// so the tree can be shown working on a large data set.
        /// </summary>
        public int SeedHistory(string sensorId, int count, string unit)
        {
            var random = new Random();
            var now = DateTime.UtcNow;
            var value = unit == "W" ? 1500.0 : 55.0;
            var step = unit == "W" ? 60.0 : 1.5;

            for (int i = count; i > 0; i--)
            {
                value = Math.Max(1, value + (random.NextDouble() * 2 - 1) * step);

                Add(new TelemetryPacket<double>
                {
                    SensorId = sensorId,
                    Timestamp = now.AddMinutes(-i),
                    Value = Math.Round(value, 1),
                    Unit = unit
                });
            }

            return count;
        }
    }
}