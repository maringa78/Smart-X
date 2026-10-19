using SmartX.Api.Models;

namespace SmartX.API.Services
{
    /// <summary>
    /// Holds telemetry packets between the moment they hit the API and the moment
    /// they are processed. Two structures work together:
    ///   - Queue&lt;T&gt;         : normal packets, strict first-in-first-out.
    ///   - PriorityQueue&lt;T&gt; : critical packets, which are always served first.
    /// The API thread only enqueues (fast); a background worker drains the pipeline.
    /// </summary>
    public class TelemetryPipeline
    {
        private readonly Queue<TelemetryPacket<double>> _standard = new();
        private readonly PriorityQueue<TelemetryPacket<double>, int> _critical = new();
        private readonly object _lock = new();

        // Lower number = more urgent (PriorityQueue dequeues the smallest priority first).
        public const int PowerSpikePriority = 0;
        public const int MoistureDropPriority = 1;

        // Critical thresholds (also used by the suggestion engine to spot devices nearing them).
        public const double PowerSpikeWatts = 3000;
        public const double MoistureCriticalPercent = 20;

        /// <summary>Decides whether a packet is critical, and how urgent it is.</summary>
        public static bool TryGetCriticalPriority(TelemetryPacket<double> packet, out int priority)
        {
            switch (packet.Unit)
            {
                case "W" when packet.Value > PowerSpikeWatts:       // severe power spike
                    priority = PowerSpikePriority;
                    return true;
                case "%" when packet.Value < MoistureCriticalPercent:         // critical moisture drop
                    priority = MoistureDropPriority;
                    return true;
                default:
                    priority = int.MaxValue;
                    return false;
            }
        }

        /// <summary>Adds a packet. Returns true if it was routed to the priority queue.</summary>
        public bool Enqueue(TelemetryPacket<double> packet)
        {
            lock (_lock)
            {
                if (TryGetCriticalPriority(packet, out var priority))
                {
                    _critical.Enqueue(packet, priority);
                    return true;
                }

                _standard.Enqueue(packet);
                return false;
            }
        }

        /// <summary>Critical packets always bypass the standard queue.</summary>
        public bool TryDequeue(out TelemetryPacket<double>? packet, out bool wasCritical)
        {
            lock (_lock)
            {
                if (_critical.TryDequeue(out packet, out _))
                {
                    wasCritical = true;
                    return true;
                }

                wasCritical = false;
                return _standard.TryDequeue(out packet);
            }
        }

        public (int Standard, int Critical) Counts()
        {
            lock (_lock) return (_standard.Count, _critical.Count);
        }
    }
}