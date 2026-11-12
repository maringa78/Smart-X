namespace SmartX.API.Services
{
    /// <summary>One direction of a wireless link, as stored in a node's neighbour list.</summary>
    public record MeshEdge(string To, double Cost);

    /// <summary>A wireless link between two nodes (listed once, not once per direction).</summary>
    public record MeshLink(string From, string To, double Cost);

    /// <summary>
    /// The physical mesh network, modelled as an undirected weighted graph.
    /// Vertices are the ESP32 nodes (plus the gateway) and edges are the wireless links.
    /// It is stored as an adjacency list: a Dictionary maps each node to the list of
    /// neighbours it can reach directly, which is compact for a sparse network like a mesh
    /// and makes "who are my neighbours?" an O(1) lookup.
    /// Each link has a cost: the relative transmit power needed for that hop
    /// (lower = cheaper). The routing algorithms use these costs.
    /// </summary>
    public class MeshGraph
    {
        public const string GatewayId = "GATEWAY";

        private readonly Dictionary<string, List<MeshEdge>> _adjacency = new();
        private readonly HashSet<string> _downNodes = new();
        private readonly object _lock = new();

        public MeshGraph()
        {
            SeedDemoTopology();
        }

        // ---------- building the graph ----------

        /// <summary>Adds a bidirectional link. Re-adding an existing link updates its cost.</summary>
        public void AddLink(string a, string b, double cost)
        {
            if (a == b) throw new ArgumentException("A node cannot link to itself.");
            if (cost <= 0) throw new ArgumentOutOfRangeException(nameof(cost), "Cost must be positive.");

            lock (_lock)
            {
                SetEdge(EdgesOf(a), b, cost);
                SetEdge(EdgesOf(b), a, cost);
            }
        }

        private List<MeshEdge> EdgesOf(string id)
        {
            if (!_adjacency.TryGetValue(id, out var edges))
                _adjacency[id] = edges = new List<MeshEdge>();
            return edges;
        }

        private static void SetEdge(List<MeshEdge> edges, string to, double cost)
        {
            var index = edges.FindIndex(e => e.To == to);
            if (index >= 0) edges[index] = new MeshEdge(to, cost);
            else edges.Add(new MeshEdge(to, cost));
        }

        // ---------- reading the graph ----------

        public bool Contains(string id)
        {
            lock (_lock) return _adjacency.ContainsKey(id);
        }

        /// <summary>All node IDs, gateway first, then the rest in order.</summary>
        public List<string> Nodes()
        {
            lock (_lock)
                return _adjacency.Keys
                    .OrderBy(id => id == GatewayId ? 0 : 1)
                    .ThenBy(id => id, StringComparer.Ordinal)
                    .ToList();
        }

        /// <summary>A node's direct neighbours (a copy, so callers can use it safely).</summary>
        public List<MeshEdge> Neighbours(string id)
        {
            lock (_lock)
                return _adjacency.TryGetValue(id, out var edges)
                    ? new List<MeshEdge>(edges)
                    : new List<MeshEdge>();
        }

        /// <summary>Every link once, even though it is stored in both directions.</summary>
        public List<MeshLink> Links()
        {
            lock (_lock)
                return _adjacency
                    .SelectMany(pair => pair.Value
                        .Where(e => string.CompareOrdinal(pair.Key, e.To) < 0)
                        .Select(e => new MeshLink(pair.Key, e.To, e.Cost)))
                    .OrderBy(l => l.From, StringComparer.Ordinal)
                    .ThenBy(l => l.To, StringComparer.Ordinal)
                    .ToList();
        }

        // ---------- node up / down state ----------

        public bool IsDown(string id)
        {
            lock (_lock) return _downNodes.Contains(id);
        }

        public List<string> DownNodes()
        {
            lock (_lock) return _downNodes.OrderBy(id => id, StringComparer.Ordinal).ToList();
        }

        /// <summary>Marks a node as failed. The gateway cannot be marked down. Returns false if not allowed.</summary>
        public bool MarkDown(string id)
        {
            lock (_lock)
            {
                if (id == GatewayId || !_adjacency.ContainsKey(id)) return false;
                _downNodes.Add(id);
                return true;
            }
        }

        public bool MarkUp(string id)
        {
            lock (_lock)
            {
                if (!_adjacency.ContainsKey(id)) return false;
                _downNodes.Remove(id);
                return true;
            }
        }

        // ---------- demo topology ----------

        /// <summary>
        /// The nodes match the simulator's devices. The links form several loops, so there are
        /// alternative routes when a node fails. Costs are the relative power needed per hop.
        /// </summary>
        private void SeedDemoTopology()
        {
            const string g = GatewayId;
            const string n11 = "AA:BB:CC:DD:EE:11", n12 = "AA:BB:CC:DD:EE:12", n13 = "AA:BB:CC:DD:EE:13"; // hydroponics
            const string n21 = "AA:BB:CC:DD:EE:21", n22 = "AA:BB:CC:DD:EE:22", n23 = "AA:BB:CC:DD:EE:23"; // power meters
            const string n31 = "AA:BB:CC:DD:EE:31", n32 = "AA:BB:CC:DD:EE:32";                             // greenhouse

            // Direct links to the gateway
            AddLink(g, n21, 2);
            AddLink(g, n31, 3);
            AddLink(g, n11, 4);

            // Links between nodes
            AddLink(n21, n22, 2);
            AddLink(n21, n11, 3);
            AddLink(n21, n31, 5);
            AddLink(n22, n23, 3);
            AddLink(n22, n12, 4);
            AddLink(n31, n32, 2);
            AddLink(n11, n12, 2);
            AddLink(n11, n13, 3);
            AddLink(n12, n13, 2);
            AddLink(n23, n12, 6);
            AddLink(n32, n13, 4);
        }
    }
}