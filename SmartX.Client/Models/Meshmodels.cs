namespace SmartX.Client.Models;

// ----- The mesh network (GET /api/mesh) -----

public class MeshNodeDto
{
    public string Id { get; set; } = string.Empty;
    public bool IsGateway { get; set; }
    public string? Location { get; set; }
    public bool IsDown { get; set; }
}

public class MeshLinkDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public double Cost { get; set; }
    public bool IsActive { get; set; }
}

public class MeshResponse
{
    public List<MeshNodeDto> Nodes { get; set; } = new();
    public List<MeshLinkDto> Links { get; set; } = new();
}

// ----- Network health, from BFS / DFS (GET /api/mesh/health) -----

public class NodeRouteDto
{
    public string NodeId { get; set; } = string.Empty;
    public int Hops { get; set; }
    public List<string> Path { get; set; } = new();
}

public class RerouteDto
{
    public string NodeId { get; set; } = string.Empty;
    public List<string> NormalPath { get; set; } = new();
    public List<string> CurrentPath { get; set; } = new();
}

public class NetworkHealthDto
{
    public List<NodeRouteDto> Connected { get; set; } = new();
    public List<string> DownNodes { get; set; } = new();
    public List<string> IsolatedNodes { get; set; } = new();
    public List<List<string>> IsolatedGroups { get; set; } = new();
    public List<RerouteDto> Rerouted { get; set; } = new();
}

public class RouteInfoDto
{
    public string NodeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;   // Normal, Rerouted, Isolated or Down
    public int? Hops { get; set; }
    public string? NextHop { get; set; }
    public List<string> CurrentPath { get; set; } = new();
    public List<string> NormalPath { get; set; } = new();
}

// ----- Power-efficient routing, from the minimum spanning tree (GET /api/mesh/mst) -----

public class MstEdgeDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public double Cost { get; set; }
}

public class MstRouteDto
{
    public string NodeId { get; set; } = string.Empty;
    public int Hops { get; set; }
    public double PowerCost { get; set; }
    public List<string> Path { get; set; } = new();
}

public class SpanningTreeDto
{
    public List<MstEdgeDto> Edges { get; set; } = new();
    public double TotalCost { get; set; }
    public double AllLinksCost { get; set; }
    public double PowerSaved { get; set; }
    public double SavedPercent { get; set; }
    public List<MstRouteDto> Routes { get; set; } = new();
    public List<string> Excluded { get; set; } = new();
}

// ----- Historical readings, from the AVL-tree archive (GET /api/archive/{sensorId}) -----

public class ArchiveStats
{
    public int TotalReadings { get; set; }
    public int NodesVisited { get; set; }
    public int TreeHeight { get; set; }
}

public class ArchiveResponse
{
    public string SensorId { get; set; } = string.Empty;
    public int Count { get; set; }
    public ArchiveStats Stats { get; set; } = new();
    public List<TelemetryPacket> Readings { get; set; } = new();
}