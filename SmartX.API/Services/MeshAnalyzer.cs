namespace SmartX.API.Services
{
  
    public record NodeRoute(string NodeId, int Hops, List<string> Path);

    public record RerouteInfo(string NodeId, List<string> NormalPath, List<string> CurrentPath);

    public record NetworkHealth(
        List<NodeRoute> Connected,          // working nodes that can still reach the gateway
        List<string> DownNodes,             // failed nodes
        List<string> IsolatedNodes,         // working nodes that have lost every route to the gateway
        List<List<string>> IsolatedGroups,  // those isolated nodes grouped by who can still reach whom
        List<RerouteInfo> Rerouted);        // connected nodes that had to change route

    public record RouteInfo(
        string NodeId,
        string Status,                      // Normal, Rerouted, Isolated or Down
        int? Hops,
        string? NextHop,                    // where this node should send its data next
        List<string> CurrentPath,
        List<string> NormalPath);

  
    public static class MeshAnalyzer
    {
        
        private static Dictionary<string, string?> Bfs(MeshGraph graph, string start, HashSet<string> blocked)
        {
            var cameFrom = new Dictionary<string, string?> { [start] = null };
            var queue = new Queue<string>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var edge in graph.Neighbours(current))
                {
                    if (blocked.Contains(edge.To) || cameFrom.ContainsKey(edge.To)) continue;

                    cameFrom[edge.To] = current;
                    queue.Enqueue(edge.To);
                }
            }

            return cameFrom;
        }

        /// <summary>Walks back from a node to the BFS start: [node, ..., start].</summary>
        private static List<string> PathBack(string node, Dictionary<string, string?> cameFrom)
        {
            var path = new List<string>();
            for (string? current = node; current is not null; current = cameFrom[current])
                path.Add(current);
            return path;
        }

        // ---------- DFS ----------

        /// <summary>
        /// Groups the given nodes into islands: sets of nodes that can reach each other directly
        /// or through each other. Uses an explicit Stack for depth-first search.
        /// </summary>
        private static List<List<string>> FindIslands(MeshGraph graph, IEnumerable<string> nodes)
        {
            var remaining = new HashSet<string>(nodes);
            var islands = new List<List<string>>();

            while (remaining.Count > 0)
            {
                var start = remaining.OrderBy(id => id, StringComparer.Ordinal).First();
                remaining.Remove(start);

                var island = new List<string>();
                var stack = new Stack<string>();
                stack.Push(start);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    island.Add(current);

                    foreach (var edge in graph.Neighbours(current))
                    {
                        // Remove returns true only for a node we had not visited yet.
                        if (remaining.Remove(edge.To)) stack.Push(edge.To);
                    }
                }

                islands.Add(island);
            }

            return islands;
        }

        // ---------- public analysis ----------

        /// <summary>Whole-network health: who is connected, who is down, who is cut off, and who detours.</summary>
        public static NetworkHealth Analyze(MeshGraph graph)
        {
            var down = graph.DownNodes().ToHashSet();

            var normal = Bfs(graph, MeshGraph.GatewayId, new HashSet<string>());   // as if nothing had failed
            var current = Bfs(graph, MeshGraph.GatewayId, down);                    // the network right now

            var connected = current.Keys
                .Where(id => id != MeshGraph.GatewayId)
                .Select(id =>
                {
                    var path = PathBack(id, current);
                    return new NodeRoute(id, path.Count - 1, path);
                })
                .OrderBy(r => r.Hops)
                .ThenBy(r => r.NodeId, StringComparer.Ordinal)
                .ToList();

            var isolated = graph.Nodes()
                .Where(id => id != MeshGraph.GatewayId && !down.Contains(id) && !current.ContainsKey(id))
                .ToList();

            // A node must detour if its normal route passed through a node that is now down.
            var rerouted = new List<RerouteInfo>();
            foreach (var route in connected)
            {
                var normalPath = PathBack(route.NodeId, normal);
                if (normalPath.Any(id => down.Contains(id)))
                    rerouted.Add(new RerouteInfo(route.NodeId, normalPath, route.Path));
            }

            return new NetworkHealth(
                connected,
                down.OrderBy(id => id, StringComparer.Ordinal).ToList(),
                isolated,
                FindIslands(graph, isolated),
                rerouted);
        }

        /// <summary>Route of a single node to the gateway, or null if the node does not exist.</summary>
        public static RouteInfo? RouteFor(MeshGraph graph, string nodeId)
        {
            if (!graph.Contains(nodeId)) return null;

            var down = graph.DownNodes().ToHashSet();
            var normalPath = PathBack(nodeId, Bfs(graph, MeshGraph.GatewayId, new HashSet<string>()));

            if (down.Contains(nodeId))
                return new RouteInfo(nodeId, "Down", null, null, new List<string>(), normalPath);

            var current = Bfs(graph, MeshGraph.GatewayId, down);
            if (!current.ContainsKey(nodeId))
                return new RouteInfo(nodeId, "Isolated", null, null, new List<string>(), normalPath);

            var path = PathBack(nodeId, current);
            var detoured = normalPath.Any(id => down.Contains(id));
            var nextHop = path.Count > 1 ? path[1] : null;

            return new RouteInfo(nodeId, detoured ? "Rerouted" : "Normal", path.Count - 1, nextHop, path, normalPath);
        }

        /// <summary>
        /// Every working node reachable from the start node (for example a whole subnet),
        /// with its hop count and route back to the start. Null if the start node does not exist.
        /// </summary>
        public static List<NodeRoute>? Reachable(MeshGraph graph, string start)
        {
            if (!graph.Contains(start)) return null;

            var down = graph.DownNodes().ToHashSet();
            if (down.Contains(start)) return new List<NodeRoute>();   // a failed node reaches nothing

            var cameFrom = Bfs(graph, start, down);

            return cameFrom.Keys
                .Where(id => id != start)
                .Select(id =>
                {
                    var path = PathBack(id, cameFrom);
                    return new NodeRoute(id, path.Count - 1, path);
                })
                .OrderBy(r => r.Hops)
                .ThenBy(r => r.NodeId, StringComparer.Ordinal)
                .ToList();
        }
    }
}