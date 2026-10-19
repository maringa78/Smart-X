using SmartX.API.Models;

namespace SmartX.API.Services
{
    /// <summary>One thing that happened: an alert, a search or a command, in the order it occurred.</summary>
    public record ActionEvent(string Token, DateTime Time);

    /// <summary>A recommendation for the operator, shown in the UI as a "Suggested Action".</summary>
    public record SuggestedAction(
        string Kind,            // "Command", "Search" or "Watch"
        string Label,           // text for the button
        string Reason,          // why it is suggested
        double Confidence,      // 0 to 1
        string? DeviceId,
        string? CommandName,
        string? State,
        string? SearchTerm);

    /// <summary>A device the engine thinks needs attention, shown as an "Automated Insight".</summary>
    public record Insight(string Severity, string DeviceId, string Message);

    public record SuggestionResult(List<SuggestedAction> SuggestedActions, List<Insight> Insights);

    /// <summary>
    /// Learns from what operators do and predicts what they will want next.
    ///
    /// Every alert, search and command becomes an event token, e.g.
    ///   alert:CriticalReading:AA:BB:CC:DD:EE:11
    ///   search:pump a
    ///   command:AA:BB:CC:DD:EE:41|WaterPump|On
    /// A Queue holds the recent events (a sliding time window). Each new event is paired
    /// with the distinct events that came before it inside that window, and the pair
    /// counts go into a Dictionary of Dictionaries (a simple Markov-style transition table).
    /// To suggest, the engine looks at what is happening now (active alerts and the latest
    /// action), finds what usually followed them, and ranks the candidates with a PriorityQueue.
    /// </summary>
    public class SuggestionEngine
    {
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(3);
        private const int MinSupport = 2;          // a pattern must repeat at least this often
        private const int MaxSuggestions = 5;
        private const int MaxRecentEvents = 50;

        // earlier event -> (later event -> how many times it followed)
        private readonly Dictionary<string, Dictionary<string, int>> _transitions = new();
        // event -> how many times it has happened
        private readonly Dictionary<string, int> _occurrences = new();
        private readonly Queue<ActionEvent> _recent = new();
        private ActionEvent? _lastEvent;
        private readonly object _lock = new();

        // ---------- recording ----------

        public void RecordAlert(string deviceId, AlertType type, DateTime? at = null) =>
            Record(AlertToken(deviceId, type), at ?? DateTime.UtcNow);

        public void RecordSearch(string term, DateTime? at = null)
        {
            var clean = term.Trim().ToLowerInvariant();
            if (clean.Length > 0) Record($"search:{clean}", at ?? DateTime.UtcNow);
        }

        public void RecordCommand(Command command) =>
            Record(CommandToken(command.DeviceId, command.Name, command.State), command.IssuedAt);

        private static string AlertToken(string deviceId, AlertType type) => $"alert:{type}:{deviceId}";
        private static string CommandToken(string deviceId, string name, string state) => $"command:{deviceId}|{name}|{state}";

        private void Record(string token, DateTime at)
        {
            lock (_lock)
            {
                // Slide the window: forget events that are too old (or too many).
                while (_recent.Count > 0 &&
                       (at - _recent.Peek().Time > Window || _recent.Count >= MaxRecentEvents))
                    _recent.Dequeue();

                // Count each distinct earlier event once for this new event.
                var alreadyCounted = new HashSet<string>();
                foreach (var earlier in _recent)
                {
                    if (earlier.Token == token || !alreadyCounted.Add(earlier.Token)) continue;

                    if (!_transitions.TryGetValue(earlier.Token, out var followers))
                        _transitions[earlier.Token] = followers = new Dictionary<string, int>();

                    followers[token] = followers.GetValueOrDefault(token) + 1;
                }

                _occurrences[token] = _occurrences.GetValueOrDefault(token) + 1;

                var evt = new ActionEvent(token, at);
                _recent.Enqueue(evt);
                _lastEvent = evt;
            }
        }

        // ---------- suggesting ----------

        private record Candidate(string Token, string Context, int Count, int Occurrences, double Confidence);

        public SuggestionResult Suggest(IEnumerable<AlertKey> activeAlerts, IEnumerable<SensorRecord> devices)
        {
            // What is happening right now: every active alert, plus the operator's latest action.
            var contexts = activeAlerts.Select(a => AlertToken(a.DeviceId, a.Type)).ToHashSet();
            var candidates = new Dictionary<string, Candidate>();

            lock (_lock)
            {
                if (_lastEvent is not null && DateTime.UtcNow - _lastEvent.Time <= Window)
                    contexts.Add(_lastEvent.Token);

                foreach (var context in contexts)
                {
                    if (!_transitions.TryGetValue(context, out var followers)) continue;
                    var occurrences = _occurrences[context];

                    foreach (var (token, count) in followers)
                    {
                        // Skip one-off coincidences and things that are already happening.
                        if (count < MinSupport || contexts.Contains(token)) continue;

                        var confidence = Math.Min(1.0, (double)count / occurrences);
                        if (!candidates.TryGetValue(token, out var existing) || confidence > existing.Confidence)
                            candidates[token] = new Candidate(token, context, count, occurrences, confidence);
                    }
                }
            }

            // Rank by confidence (then by how often it was seen) using a priority queue.
            var ranking = new PriorityQueue<Candidate, (double, int)>();
            foreach (var c in candidates.Values)
                ranking.Enqueue(c, (-c.Confidence, -c.Count));

            var actions = new List<SuggestedAction>();
            while (actions.Count < MaxSuggestions && ranking.TryDequeue(out var best, out _))
                actions.Add(ToAction(best));

            return new SuggestionResult(actions, FindProblemDevices(devices));
        }

        private static SuggestedAction ToAction(Candidate c)
        {
            var reason = $"After {Describe(c.Context)}, {Describe(c.Token)} followed {c.Count} of {c.Occurrences} times.";

            if (c.Token.StartsWith("command:"))
            {
                var p = c.Token["command:".Length..].Split('|');
                return new SuggestedAction("Command", $"Set {p[1]} {p[2]} on {p[0]}",
                    reason, c.Confidence, p[0], p[1], p[2], null);
            }

            if (c.Token.StartsWith("search:"))
            {
                var term = c.Token["search:".Length..];
                return new SuggestedAction("Search", $"Search for \"{term}\"",
                    reason, c.Confidence, null, null, null, term);
            }

            // An alert on another device that tends to follow: worth checking it early.
            var a = c.Token.Split(':', 3);
            return new SuggestedAction("Watch", $"Check {a[2]}",
                reason, c.Confidence, a[2], null, null, null);
        }

        private static string Describe(string token)
        {
            if (token.StartsWith("search:"))
                return $"searching \"{token["search:".Length..]}\"";

            if (token.StartsWith("command:"))
            {
                var p = token["command:".Length..].Split('|');
                return $"setting {p[1]} {p[2]} on {p[0]}";
            }

            var a = token.Split(':', 3);   // alert:<type>:<device>
            return a[1] == nameof(AlertType.Disconnected)
                ? $"{a[2]} disconnecting"
                : $"a critical reading on {a[2]}";
        }

        // ---------- highlighting problem devices ----------

        private static List<Insight> FindProblemDevices(IEnumerable<SensorRecord> devices)
        {
            var insights = new List<Insight>();

            foreach (var d in devices)
            {
                if (d.Status == DeviceStatus.Offline)
                    insights.Add(new("Critical", d.MacAddress,
                        $"{d.DeploymentLocation} is offline (last seen {d.LastSeen:HH:mm:ss} UTC)."));
                else if (d.Status == DeviceStatus.Alert)
                    insights.Add(new("Critical", d.MacAddress,
                        $"{d.DeploymentLocation} reported a critical reading ({d.LastValue} {d.LastUnit})."));
                else if (IsNearThreshold(d))
                    insights.Add(new("Warning", d.MacAddress,
                        $"{d.DeploymentLocation} is approaching its critical limit ({d.LastValue} {d.LastUnit})."));
            }

            return insights
                .OrderBy(i => i.Severity == "Critical" ? 0 : 1)
                .ThenBy(i => i.DeviceId)
                .ToList();
        }

        // Within 10% of the power limit, or within 5 points of the moisture limit.
        private static bool IsNearThreshold(SensorRecord d) =>
            d.Status == DeviceStatus.Online &&
            d.LastValue is double v &&
            (d.LastUnit switch
            {
                "W" => v > TelemetryPipeline.PowerSpikeWatts * 0.9,
                "%" => v < TelemetryPipeline.MoistureCriticalPercent + 5,
                _ => false
            });

        // ---------- demo helper ----------

        /// <summary>
        /// Fills the engine with simulated operator history, so the patterns can be shown
        /// without hours of real use: a moisture drop on one sensor is usually followed by
        /// searching for the pump and switching it on, and a power spike on a meter by
        /// switching its relay off.
        /// </summary>
        public void SeedDemoHistory()
        {
            var start = DateTime.UtcNow.AddHours(-2);
            var events = new List<(string Token, DateTime At)>();

            for (int i = 0; i < 4; i++)
            {
                var t = start.AddMinutes(i * 15);
                events.Add((AlertToken("AA:BB:CC:DD:EE:11", AlertType.CriticalReading), t));
                events.Add(("search:pump a", t.AddSeconds(20)));
                events.Add((CommandToken("AA:BB:CC:DD:EE:41", "WaterPump", "On"), t.AddSeconds(45)));
            }

            for (int i = 0; i < 3; i++)
            {
                var t = start.AddMinutes(7 + i * 15);
                events.Add((AlertToken("AA:BB:CC:DD:EE:21", AlertType.CriticalReading), t));
                events.Add((CommandToken("AA:BB:CC:DD:EE:21", "Relay1", "Off"), t.AddSeconds(30)));
            }

            foreach (var (token, at) in events.OrderBy(e => e.At))
                Record(token, at);
        }
    }
}