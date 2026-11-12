namespace SmartX.API.Services
{
  
    public record MstEdge(string From, string To, double Cost);

 
    public record MstRoute(string NodeId, int Hops, double PowerCost, List<string> Path);

    public record SpanningTreeResult(
        List<MstEdge> Edges,        // the links the backbone uses
        double TotalCost,           // power cost of the backbone (sum of its links)
        double AllLinksCost,        // power cost if every working link were used
        double PowerSaved,          // AllLinksCost - TotalCost
        double SavedPercent,
        List<MstRoute> Routes,      // route from every connected node to the gateway
        List<string> Excluded);     // nodes left out because they are down or cut off

   
    public static class MeshRouting
    {
        public static SpanningTreeResult ComputeMst(MeshGraph graph)
        {
            var down = graph.DownNodes().ToHashSet();

            var inTree = new HashSet<string> { MeshGraph.GatewayId };
            var edges = new List<MstEdge>();
            var candidates = new PriorityQueue<MstEdge, double>();   // min-heap ordered by link cost

            void AddCandidates(string node)
            {
                foreach (var link in graph.Neighbours(node))
                {
                    if (down.Contains(link.To) || inTree.Contains(link.To)) continue;
                    candidates.Enqueue(new MstEdge(node, link.To, link.Cost), link.Cost);
                }
            }

            AddCandidates(MeshGraph.GatewayId);

            while (candidates.Count > 0)
            {
                var cheapest = candidates.Dequeue();

                // The node may already have been reached through a cheaper link.
                if (inTree.Contains(cheapest.To)) continue;

                inTree.Add(cheapest.To);
                edges.Add(cheapest);
                AddCandidates(cheapest.To);
            }

            // Build each node's route by walking up the tree to the gateway.
            var parent = edges.ToDictionary(e => e.To, e => e);
            var routes = new List<MstRoute>();

            foreach (var edge in edges)
            {
                var path = new List<string> { edge.To };
                double power = 0;

                var node = edge.To;
                while (parent.TryGetValue(node, out var up))
                {
                    power += up.Cost;
                    path.Add(up.From);
                    node = up.From;
                }

                routes.Add(new MstRoute(edge.To, path.Count - 1, power, path));
            }

            var totalCost = edges.Sum(e => e.Cost);
            var allLinksCost = graph.Links()
                .Where(l => inTree.Contains(l.From) && inTree.Contains(l.To))
                .Sum(l => l.Cost);
            var saved = allLinksCost - totalCost;

            var excluded = graph.Nodes().Where(id => !inTree.Contains(id)).ToList();

            return new SpanningTreeResult(
                edges,
                totalCost,
                allLinksCost,
                saved,
                allLinksCost == 0 ? 0 : Math.Round(saved / allLinksCost * 100, 1),
                routes.OrderBy(r => r.Hops).ThenBy(r => r.NodeId, StringComparer.Ordinal).ToList(),
                excluded);
        }

        /// <summary>One node's route from an already calculated tree, or null if it is not in the tree.</summary>
        public static MstRoute? RouteFor(SpanningTreeResult tree, string nodeId)
        {
            if (nodeId == MeshGraph.GatewayId)
                return new MstRoute(nodeId, 0, 0, new List<string> { nodeId });

            return tree.Routes.FirstOrDefault(r => r.NodeId == nodeId);
        }
    }
}