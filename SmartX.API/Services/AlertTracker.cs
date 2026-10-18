namespace SmartX.API.Services
{
    public enum AlertType
    {
        Disconnected,      // device stopped reporting
        CriticalReading    // device sent a critical value (power spike, moisture drop)
    }

    /// <summary>One alert = one device + one kind of problem. Records compare by value.</summary>
    public readonly record struct AlertKey(string DeviceId, AlertType Type);

    /// <summary>
    /// Tracks which alerts are active right now using a HashSet&lt;T&gt;.
    /// A set only keeps unique items, so raising an alert that is already active
    /// is detected by HashSet.Add returning false. That is how duplicate
    /// disconnect or critical alerts for the same sensor are ignored.
    /// </summary>
    public class AlertTracker
    {
        private readonly HashSet<AlertKey> _active = new();
        private readonly object _lock = new();
        private int _suppressedDuplicates;

        /// <summary>Returns true if this alert is new, false if it was already active.</summary>
        public bool Raise(string deviceId, AlertType type)
        {
            lock (_lock)
            {
                var isNew = _active.Add(new AlertKey(deviceId, type));
                if (!isNew) _suppressedDuplicates++;
                return isNew;
            }
        }

        /// <summary>Removes an alert once the problem is resolved.</summary>
        public bool Clear(string deviceId, AlertType type)
        {
            lock (_lock) return _active.Remove(new AlertKey(deviceId, type));
        }

        public List<AlertKey> Active()
        {
            lock (_lock) return _active.ToList();
        }

        /// <summary>How many repeat alerts the set has blocked (handy for the demo).</summary>
        public int SuppressedDuplicates
        {
            get { lock (_lock) return _suppressedDuplicates; }
        }
    }
}