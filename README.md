# Smart-X Gateway

An industrial IoT gateway and real-time management dashboard for monitoring distributed sensor networks (hydroponics farms, smart-grid meters, and greenhouse facilities). Devices transmit telemetry to a .NET minimal API, which queues, validates, archives, and routes packets across an interactive self-healing mesh topology. A Blazor dashboard provides live telemetry feeds, command dispatch, and interactive network visualization.

- **GitHub Repository**: https://github.com/EMWCCN/PROG7312_POE_Gr2_St10287116.git
- **YouTube Demo (Part 1)**: https://www.youtube.com/watch?v=62TH5MTy8qI
- **YouTube Demo (Part 2)**: https://youtu.be/WAZFYqw265o

---

## Contents

1. [Project Overview](#project-overview)
2. [Prerequisites](#prerequisites)
3. [Project Structure](#project-structure)
4. [Setup and Running](#setup-and-running)
5. [Using the Application](#using-the-application)
6. [Data Structures & Detailed Code Explanations](#data-structures--detailed-code-explanations)
   - [FIFO Queue & Min-Priority Queue (`TelemetryPipeline`)](#1-fifo-queue--priority-queue-telemetrypipeline)
   - [LIFO Stack with Undo History (`CommandHistory`)](#2-lifo-stack-with-state-undo-commandhistory)
   - [Hash Table Dictionary for O(1) Registries (`SensorRegistry`)](#3-hash-table-dictionary-sensorregistry)
   - [HashSet for Duplicate-Free Alert Tracking (`AlertTracker`)](#4-hashset-for-deduplication-alerttracker)
   - [Self-Balancing AVL Binary Search Tree (`AvlTree`, `TelemetryArchive`)](#5-self-balancing-avl-tree-avltree-telemetryarchive)
   - [Weighted Undirected Graph with Adjacency List (`MeshGraph`)](#6-weighted-undirected-graph-meshgraph)
   - [Breadth-First Search (BFS) for Detour Routing (`MeshAnalyzer`)](#7-breadth-first-search-bfs-meshanalyzer)
   - [Depth-First Search (DFS) for Island Partitioning (`MeshAnalyzer`)](#8-depth-first-search-dfs-for-network-islands)
   - [Prim's Minimum Spanning Tree Algorithm (`MeshRouting`)](#9-prims-minimum-spanning-tree-mst-meshrouting)
   - [Sliding Window Queue & Predictive Association Matrix (`SuggestionEngine`)](#10-sliding-window-queue--predictive-association-matrix-suggestionengine)
7. [API Endpoints Reference](#api-endpoints-reference)
8. [Simulator Reference](#simulator-reference)
9. [Troubleshooting](#troubleshooting)

---

## Project Overview

Smart-X is composed of three interconnected projects:

| Project | Runtime / Type | Purpose |
|---|---|---|
| `SmartX.API` | ASP.NET Core 10 Minimal API | Ingests telemetry, executes asynchronous queues, stores AVL archives, manages commands, and computes graph routing. |
| `SmartX.Client` | Blazor Server (.NET 10) | Interactive dashboard. Renders telemetry streams, live command history, and SVG mesh topology routing. |
| `SmartX.Simulator` | .NET 10 Console App | Simulates a distributed fleet of IoT sensors (ESP32 microcontrollers). Sends real-time telemetry, bursts, and disconnect events over HTTP. |

All data is held in-memory with deterministic data structures.

---

## Prerequisites

- **.NET SDK 10.0 or higher**: https://dotnet.microsoft.com/download (check with `dotnet --version`)
- **Visual Studio 2022 / 2025** or VS Code with C# Dev Kit
- **Git**
- A trusted development HTTPS certificate:
  ```powershell
  dotnet dev-certs https --trust
  ```

---

## Project Structure

```
SmartX.API/
├── SmartX.API/                        # Backend REST Minimal API
│   ├── Program.cs                     # Endpoint mapping & dependency injection
│   ├── SensorRegistry.cs              # O(1) MAC address dictionary & device registry
│   ├── Models/                        # Domain models (SensorRecord, TelemetryPacket, etc.)
│   └── Services/
│       ├── AvlTree.cs                 # Generic self-balancing AVL binary search tree
│       ├── TelemetryArchive.cs        # AVL-backed historical sensor time-range store
│       ├── MeshGraph.cs               # Weighted undirected graph adjacency list
│       ├── MeshRouting.cs             # Prim's MST minimum-power backbone routing
│       ├── MeshAnalyzer.cs            # BFS shortest paths & DFS island cluster detection
│       ├── TelemetryPipeline.cs       # FIFO standard queue + PriorityQueue for criticals
│       ├── TelemetryProcessor.cs      # Background worker draining the ingestion queues
│       ├── CommandHistory.cs          # LIFO stack with state reversion (undo)
│       ├── AlertTracker.cs            # HashSet deduplication for active alert states
│       ├── DeviceMonitor.cs           # Heartbeat watcher marking silent nodes offline
│       └── SuggestionEngine.cs        # Temporal sliding window association matrix
│
├── SmartX.Client/                     # Blazor Server Web Application
│   ├── Components/
│   │   ├── Layout/                    # MainLayout, topbar, NavMenu, IntroLandingModal
│   │   └── Pages/
│   │       ├── Home.razor             # Landing dashboard with pillar navigation & filters
│   │       ├── Sensors.razor          # Sensor provisioning & telemetry feed
│   │       ├── Commands.razor         # Real-time command stream & undo stack UI
│   │       └── NetworkTopology.razor  # Interactive SVG mesh graph, MST backbone & detours
│   └── Services/
│       ├── AppState.cs                # Scoped session state container across pages
│       └── SmartXApiClient.cs         # Asynchronous HTTP API client service
│
└── SmartX.Simulator/                  # IoT Device Fleet Simulator
    └── Program.cs                     # Interactive console triggering spikes, drops, & bursts
```

---

## Setup and Running

### Option 1: Visual Studio
1. Open `SmartX.API.slnx` in Visual Studio.
2. Set multiple startup projects (`SmartX.API` and `SmartX.Client`).
3. Press **F5** to start both projects.

### Option 2: Command Line (PowerShell)
Open three separate terminal windows:

```powershell
# Terminal 1: Backend API
cd SmartX.API
dotnet run

# Terminal 2: Client Dashboard
cd SmartX.Client
dotnet run

# Terminal 3: Hardware Simulator
cd SmartX.Simulator
dotnet run
```

Access the client application at `https://localhost:7142` (or the port displayed in your terminal).
Access Swagger API documentation at `https://localhost:7008/swagger`.

---

## Using the Application

### 1. Launch Landing Experience
When the client application opens, an introductory landing modal introduces the three core architectural pillars:
- **Pillar 01: Sensor Data Ingestion & Telemetry**
- **Pillar 02: Real-Time Command Stream & History**
- **Pillar 03: Network Topology & Mesh Routing**

Clicking any pillar navigates directly to that section.

### 2. Sensor Data Ingestion (`/sensors`)
- Provision new IoT hardware by entering its MAC address, deployment location, and sensor category.
- Inspect registered nodes, attach log files or images, and observe live incoming telemetry packets.

### 3. Real-Time Command Stream (`/commands`)
- View aggregated fleet health metrics (Online, Alert, Offline, Queued).
- Filter devices by search term, category, and status.
- Issue manual commands (`WaterPump`, `Ventilation`, `Relay`) with immediate execution.
- Click **Undo Last Command** to pop the top command off the stack and restore previous hardware state.
- Inspect predictive suggested actions and automated rule insights.

### 4. Network Topology & Mesh Routing (`/topology`)
- Visualizes the full IoT mesh graph rendered via interactive SVG.
- Displays node hop latency and transmission costs along with the Prim MST backbone.
- Click any node to track its active route, next-hop gateway path, or mark it as down/recovered to watch dynamic BFS rerouting and detour discovery in real time.
- Query historical sensor data across customizable time windows powered by the AVL tree engine.

---

## Data Structures & Detailed Code Explanations

Each data structure in Smart-X was intentionally selected to meet real-time IoT architectural requirements: bounded latency, O(1) key lookups, deterministic rebalancing, and optimal network pathfinding.

---

### 1. FIFO Queue & Priority Queue (`TelemetryPipeline`)

#### Conceptual Explanation
Telemetry data arrives at high velocity from hundreds of microcontrollers. A standard first-in, first-out (FIFO) queue buffers normal readings without blocking the HTTP request thread. However, critical safety anomalies (such as extreme power surges or dangerous moisture depletion) cannot wait behind hundreds of standard routine packets. 

To solve this, `TelemetryPipeline` integrates two distinct queue structures:
1. `Queue<TelemetryPacket>`: standard FIFO queue handling normal periodic measurements.
2. `PriorityQueue<TelemetryPacket, int>`: min-heap priority queue where smaller priority numbers indicate greater urgency (e.g., Priority 0 for critical events).

The background worker (`TelemetryProcessor`) continuously checks the priority queue first; normal queue packets are only dequeued when no high-priority safety alerts are pending.

```
Incoming Telemetry
       │
       ├── Is Critical? ────► [ PriorityQueue (Min-Heap) ] ──┐
       │                                                      ├─► TelemetryProcessor Worker
       └────────────────────► [ Standard FIFO Queue      ] ──┘
```

#### Code Implementation
```csharp
// Source: SmartX.API/Services/TelemetryPipeline.cs
public class TelemetryPipeline
{
    private readonly Queue<TelemetryPacket> _standardQueue = new();
    private readonly PriorityQueue<TelemetryPacket, int> _criticalQueue = new();
    private readonly object _lock = new();

    public bool Enqueue(TelemetryPacket packet)
    {
        var critical = IsCritical(packet);
        lock (_lock)
        {
            if (critical)
            {
                // Priority 0 = processed ahead of normal queue packets
                _criticalQueue.Enqueue(packet, 0);
            }
            else
            {
                _standardQueue.Enqueue(packet);
            }
        }
        return critical;
    }

    public TelemetryPacket? Dequeue()
    {
        lock (_lock)
        {
            // Always drain safety-critical alerts first
            if (_criticalQueue.TryDequeue(out var criticalItem, out _))
                return criticalItem;

            if (_standardQueue.TryDequeue(out var normalItem))
                return normalItem;

            return null;
        }
    }
}
```

---

### 2. LIFO Stack with State Undo (`CommandHistory`)

#### Conceptual Explanation
When operators dispatch remote control signals to field actuators (e.g., turning a water pump on or closing an electrical breaker), accidental or erroneous actions must be immediately reversible. 

A Last-In, First-Out (`Stack<CommandRecord>`) stores command invocations. Crucially, each record captures the **previous hardware state** it displaced. When the operator triggers `Undo`, the newest command is popped off the top of the stack, and its recorded previous state is reinstated.

```
       Push Command                  Pop (Undo)
       ┌───────────┐                ┌───────────┐
       │ Command 3 │ ◄── Stack Top ──► │ Restored! │
       ├───────────┤                └───────────┘
       │ Command 2 │
       ├───────────┤
       │ Command 1 │
       └───────────┘
```

#### Code Implementation
```csharp
// Source: SmartX.API/Services/CommandHistory.cs
public class CommandHistory
{
    private readonly Stack<CommandRecord> _history = new();
    private readonly Dictionary<string, string> _latestState = new();
    private readonly object _lock = new();

    public CommandRecord Issue(IssueCommandRequest req)
    {
        lock (_lock)
        {
            var key = $"{req.DeviceId}:{req.Name}";
            var previous = _latestState.TryGetValue(key, out var state) ? state : "Off";

            var record = new CommandRecord(
                Guid.NewGuid().ToString("N")[..8],
                req.DeviceId,
                req.Name,
                req.State,
                previous,
                DateTime.UtcNow
            );

            _latestState[key] = req.State;
            _history.Push(record); // Top of the LIFO stack
            return record;
        }
    }

    public CommandRecord? Undo()
    {
        lock (_lock)
        {
            if (_history.Count == 0) return null;

            var undone = _history.Pop(); // Retrieve newest command
            var key = $"{undone.DeviceId}:{undone.Name}";
            
            // Revert state back to what existed before that command was issued
            _latestState[key] = undone.PreviousState;
            return undone;
        }
    }
}
```

---

### 3. Hash Table Dictionary (`SensorRegistry`)

#### Conceptual Explanation
Distributed IoT networks require instantaneous identification of incoming sensor packets based on hardware MAC addresses. Linear array searching exhibits $O(n)$ complexity, which degrades as thousands of devices join.

`SensorRegistry` utilizes `Dictionary<string, SensorRecord>`, an internal hash table providing **$O(1)$ constant time** lookup, insertion, and updates. MAC addresses serve as unique hash keys, enabling instant resolution of hardware locations and current statuses.

#### Code Implementation
```csharp
// Source: SmartX.API/SensorRegistry.cs
public class SensorRegistry
{
    // O(1) hash map indexed by physical MAC address
    private readonly Dictionary<string, SensorRecord> _sensors = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public bool Register(SensorRecord sensor)
    {
        lock (_lock)
        {
            if (_sensors.TryGetValue(sensor.MacAddress, out var existing))
            {
                // Update existing registered profile in O(1)
                existing.DeploymentLocation = sensor.DeploymentLocation;
                existing.Category = sensor.Category;
                return false;
            }

            _sensors[sensor.MacAddress] = sensor;
            return true;
        }
    }

    public SensorRecord? Get(string mac)
    {
        lock (_lock)
        {
            return _sensors.TryGetValue(mac, out var record) ? record : null;
        }
    }
}
```

---

### 4. HashSet for Deduplication (`AlertTracker`)

#### Conceptual Explanation
When a sensor enters an abnormal condition (e.g., temperature exceeding threshold), it may transmit identical alert packets dozens of times per second. Flooding the dashboard and operator logs with duplicate alerts obscures genuine issues.

`AlertTracker` utilizes a `HashSet<AlertRecord>`. Because hash sets enforce strict uniqueness based on hash codes, calling `Add(alert)` returns `false` if that exact alert condition is already active. This drops duplicates in $O(1)$ time while maintaining a statistical counter of suppressed duplicates.

#### Code Implementation
```csharp
// Source: SmartX.API/Services/AlertTracker.cs
public record AlertRecord(string SensorId, AlertType Type);

public class AlertTracker
{
    private readonly HashSet<AlertRecord> _activeAlerts = new();
    private int _suppressedDuplicates;
    private readonly object _lock = new();

    public bool Raise(string sensorId, AlertType type)
    {
        var alert = new AlertRecord(sensorId, type);
        lock (_lock)
        {
            // Returns false if already present in the set
            if (_activeAlerts.Add(alert))
            {
                return true; // Newly raised unique alert
            }

            _suppressedDuplicates++;
            return false; // Suppressed redundant alert
        }
    }

    public bool Clear(string sensorId, AlertType type)
    {
        lock (_lock)
        {
            return _activeAlerts.Remove(new AlertRecord(sensorId, type));
        }
    }
}
```

---

### 5. Self-Balancing AVL Tree (`AvlTree`, `TelemetryArchive`)

#### Conceptual Explanation
Historical sensor analysis frequently demands date-range queries (e.g., "fetch all temperature readings between 14:00 and 16:00"). Standard sequential lists require scanning all $N$ readings ($O(N)$). Standard binary search trees (BSTs) can degrade into skewed linked lists ($O(N)$) when timestamps arrive in chronological order.

The **AVL Tree** is a self-balancing binary search tree. For every node, the height difference between left and right subtrees (the balance factor) never exceeds 1. If an insertion creates an imbalance ($\pm 2$), the tree executes single or double rotations (**Left-Left**, **Right-Right**, **Left-Right**, or **Right-Left**).

This guarantees the tree height remains strictly bounded at $\approx \log_2(N)$, enabling both insertions and range filtering in **$O(\log N + K)$** time (where $K$ is the number of matching results).

```
         Imbalanced (Right-Right)                Balanced via Left Rotation
                 (20)                                      (30)
                   \                                      /    \
                   (30)          Rotate Left            (20)   (40)
                     \          ────────────►
                     (40)
```

#### Code Implementation
```csharp
// Source: SmartX.API/Services/AvlTree.cs
public class AvlTree<TKey, TValue> where TKey : IComparable<TKey>
{
    private class Node(TKey key, TValue value)
    {
        public TKey Key = key;
        public TValue Value = value;
        public int Height = 1;
        public Node? Left;
        public Node? Right;
    }

    private Node? _root;

    public void Insert(TKey key, TValue value)
    {
        _root = Insert(_root, key, value);
    }

    private static Node Insert(Node? node, TKey key, TValue value)
    {
        if (node is null) return new Node(key, value);

        if (key.CompareTo(node.Key) < 0)
            node.Left = Insert(node.Left, key, value);
        else
            node.Right = Insert(node.Right, key, value);

        return Rebalance(node);
    }

    private static Node Rebalance(Node node)
    {
        UpdateHeight(node);
        var balance = BalanceOf(node);

        // Left-heavy branch
        if (balance > 1)
        {
            if (BalanceOf(node.Left!) < 0)
                node.Left = RotateLeft(node.Left!); // Left-Right case
            return RotateRight(node);
        }

        // Right-heavy branch
        if (balance < -1)
        {
            if (BalanceOf(node.Right!) > 0)
                node.Right = RotateRight(node.Right!); // Right-Left case
            return RotateLeft(node);
        }

        return node;
    }

    // Range Query: Prunes subtrees that fall outside the bounds
    public List<TValue> RangeQuery(TKey from, TKey to, out int nodesVisited)
    {
        var results = new List<TValue>();
        var visited = 0;
        Collect(_root, from, to, results, ref visited);
        nodesVisited = visited;
        return results;
    }

    private static void Collect(Node? node, TKey from, TKey to, List<TValue> results, ref int visited)
    {
        if (node is null) return;
        visited++;

        var afterFrom = from.CompareTo(node.Key) <= 0;
        var beforeTo = to.CompareTo(node.Key) >= 0;

        // Skip left subtree if current node is already smaller than the search range
        if (afterFrom) Collect(node.Left, from, to, results, ref visited);

        if (afterFrom && beforeTo) results.Add(node.Value);

        // Skip right subtree if current node is already greater than the search range
        if (beforeTo) Collect(node.Right, from, to, results, ref visited);
    }
}
```

---

### 6. Weighted Undirected Graph (`MeshGraph`)

#### Conceptual Explanation
In an industrial IoT deployment, nodes cannot always communicate directly with the root gateway due to wireless distance or physical barriers. Instead, nodes relay messages peer-to-peer across a mesh topology.

`MeshGraph` represents the network as an **undirected weighted graph** $G = (V, E)$, where vertices $V$ represent IoT nodes and edges $E$ represent wireless RF links. The edge weight denotes relative transmission power cost. The graph is stored as an **Adjacency List** (`Dictionary<string, List<MeshEdge>>`), which is space-efficient for sparse networks ($O(V + E)$) and enables immediate retrieval of a node's neighbours in $O(1)$ time.

#### Code Implementation
```csharp
// Source: SmartX.API/Services/MeshGraph.cs
public record MeshEdge(string To, double Cost);
public record MeshLink(string From, string To, double Cost);

public class MeshGraph
{
    public const string GatewayId = "GATEWAY";
    
    // Adjacency List: Node ID -> List of incident wireless links
    private readonly Dictionary<string, List<MeshEdge>> _adjacency = new();
    private readonly HashSet<string> _downNodes = new();
    private readonly object _lock = new();

    public void AddLink(string a, string b, double cost)
    {
        lock (_lock)
        {
            // Undirected graph: add edge in both directions
            SetEdge(EdgesOf(a), b, cost);
            SetEdge(EdgesOf(b), a, cost);
        }
    }

    public List<MeshEdge> Neighbours(string id)
    {
        lock (_lock)
        {
            return _adjacency.TryGetValue(id, out var edges) 
                ? edges.ToList() 
                : new List<MeshEdge>();
        }
    }
}
```

---

### 7. Breadth-First Search (BFS) (`MeshAnalyzer`)

#### Conceptual Explanation
When a mesh router or intermediate node fails, packets traveling along normal paths will be lost unless an alternate route is discovered. 

`MeshAnalyzer` executes **Breadth-First Search (BFS)** using a `Queue<string>` to discover the shortest hop-count path from any node to the gateway hub. BFS guarantees finding the minimum number of relay hops in unweighted step graphs. When computing detours during failure scenarios, failed/down nodes are passed as a blocked set, forcing the traversal to explore alternate routes around the outage.

#### Code Implementation
```csharp
// Source: SmartX.API/Services/MeshAnalyzer.cs
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
            // Disregard down nodes or already-visited nodes
            if (blocked.Contains(edge.To) || cameFrom.ContainsKey(edge.To)) 
                continue;

            cameFrom[edge.To] = current;
            queue.Enqueue(edge.To);
        }
    }

    return cameFrom;
}
```

---

### 8. Depth-First Search (DFS) for Network Islands

#### Conceptual Explanation
When multiple bridge nodes fail simultaneously, sections of the mesh network can become partitioned into "islands"—clusters of working nodes that can communicate with each other, but have lost all connectivity back to the root gateway.

`MeshAnalyzer.FindIslands` executes **Depth-First Search (DFS)** using an explicit `Stack<string>` to partition disconnected nodes into connected components. This informs network operators which subnets remain locally coordinated despite losing gateway access.

#### Code Implementation
```csharp
// Source: SmartX.API/Services/MeshAnalyzer.cs
private static List<List<string>> FindIslands(MeshGraph graph, IEnumerable<string> nodes)
{
    var remaining = new HashSet<string>(nodes);
    var islands = new List<List<string>>();

    while (remaining.Count > 0)
    {
        var start = remaining.First();
        var island = new List<string>();
        
        // Explicit stack for Depth-First Search exploration
        var stack = new Stack<string>();
        stack.Push(start);
        remaining.Remove(start);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            island.Add(node);

            foreach (var edge in graph.Neighbours(node))
            {
                if (remaining.Remove(edge.To))
                {
                    stack.Push(edge.To);
                }
            }
        }

        islands.Add(island);
    }

    return islands;
}
```

---

### 9. Prim's Minimum Spanning Tree (MST) (`MeshRouting`)

#### Conceptual Explanation
Battery-powered IoT mesh devices must conserve radio transmission energy. Routing packets across arbitrary mesh cycles wastes energy and risks packet broadcast storms.

**Prim's Algorithm** computes a **Minimum Spanning Tree (MST)** rooted at the Gateway. The algorithm maintains a set of nodes already connected to the backbone tree and utilizes a min-heap `PriorityQueue<MstEdge, double>` to repeatedly extract the lowest-cost RF link that connects an unexplored node to the growing tree.

The resulting backbone connects all reachable nodes with zero cycles while minimizing cumulative RF transmission energy across the entire facility.

```
       Mesh with Cycles (Dense)                   Prim's MST Backbone (Optimal)
              (Gateway)                                     (Gateway)
              /   |   \                                     /       \
           [3]   [8]   [2]                               [3]         [2]
            /     |     \                                 /             \
         (NodeA)─[4]──(NodeB)                         (NodeA)        (NodeB)
                                                         \
                                                         [4] -> NodeB
```

#### Code Implementation
```csharp
// Source: SmartX.API/Services/MeshRouting.cs
public static SpanningTreeResult ComputeMst(MeshGraph graph)
{
    var down = graph.DownNodes().ToHashSet();
    var inTree = new HashSet<string> { MeshGraph.GatewayId };
    var edges = new List<MstEdge>();
    
    // Min-heap prioritised by power transmission cost
    var candidates = new PriorityQueue<MstEdge, double>();

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

        // Node already incorporated via an alternate cheaper path
        if (inTree.Contains(cheapest.To)) continue;

        inTree.Add(cheapest.To);
        edges.Add(cheapest);
        AddCandidates(cheapest.To);
    }

    return new SpanningTreeResult(edges, ...);
}
```

---

### 10. Sliding Window Queue & Association Matrix (`SuggestionEngine`)

#### Conceptual Explanation
Operators frequently repeat specific operational workflows after particular alerts occur (e.g., when a moisture depletion alert fires on `Zone1`, the operator searches for `Pump A` and switches `WaterPump` to `On`).

The `SuggestionEngine` learns these operational correlations automatically using:
1. `Queue<Event>`: maintains a 3-minute sliding window of recent operator actions and system alerts.
2. `Dictionary<string, Dictionary<string, int>>`: a 2D co-occurrence frequency matrix tracking how often Event $B$ immediately followed Event $A$.
3. `PriorityQueue<SuggestedAction, double>`: ranks candidate actions by learned statistical confidence score.

#### Code Implementation
```csharp
// Source: SmartX.API/Services/SuggestionEngine.cs
public class SuggestionEngine
{
    // Sliding temporal window of events
    private readonly Queue<EventRecord> _window = new();
    
    // Correlation matrix: Trigger Event -> (Follow-up Action -> Count)
    private readonly Dictionary<string, Dictionary<string, int>> _cooccurrences = new();

    public void RecordEvent(string eventKey)
    {
        var now = DateTime.UtcNow;
        PruneExpired(now); // Remove events older than 3 minutes

        // Increment co-occurrence frequency for all preceding events in the window
        foreach (var prior in _window)
        {
            IncrementCooccurrence(prior.Key, eventKey);
        }

        _window.Enqueue(new EventRecord(eventKey, now));
    }
}
```

---

## API Endpoints Reference

### Sensors & Telemetry Pipeline
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/sensors` | List all registered IoT nodes with live health statuses |
| `POST` | `/api/sensors` | Register or update an IoT sensor device |
| `GET` | `/api/sensors/{mac}` | Retrieve details for a single sensor |
| `POST` | `/api/sensors/{mac}/upload` | Attach calibration files or log images to a sensor profile |
| `POST` | `/api/telemetry` | Submit sensor reading (returns `202 Accepted`, queued in FIFO/Priority queues) |
| `GET` | `/api/pipeline/status` | Current count of standard and critical items queued |

### Commands & Predictive Suggestions
| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/commands` | Dispatch a manual actuator command (`On` / `Off`) |
| `POST` | `/api/commands/undo` | Pop the top command from the stack and revert hardware state |
| `GET` | `/api/commands/history` | List command history in LIFO order |
| `GET` | `/api/alerts` | List deduplicated active alerts and count of suppressed duplicates |
| `GET` | `/api/devices/search` | Search devices by query, category, and status |
| `GET` | `/api/suggestions` | Retrieve AI suggested actions and automated rule insights |
| `POST` | `/api/suggestions/demo-history` | Seed operator patterns for demonstration purposes |

### AVL Historical Archive
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/archive/{sensorId}` | Execute AVL range query over `from` and `to` timestamps |
| `POST` | `/api/archive/{sensorId}/seed` | Seed thousands of historical readings to verify $O(\log N)$ performance |

### Mesh Topology, BFS & MST Routing
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/mesh` | Retrieve all graph nodes, links, and operational states |
| `POST` | `/api/mesh/nodes/{id}/down` | Mark a mesh router as failed |
| `POST` | `/api/mesh/nodes/{id}/up` | Restore a failed mesh router |
| `GET` | `/api/mesh/health` | Network connectivity report (connected, isolated groups, detours) |
| `GET` | `/api/mesh/route/{id}` | BFS shortest path and next hop to gateway |
| `GET` | `/api/mesh/mst` | Minimum Spanning Tree backbone computed via Prim's algorithm |
| `GET` | `/api/mesh/mst/route/{id}` | Minimum-power backbone path from a specific node to gateway |

---

## Simulator Reference

The `SmartX.Simulator` application models an active fleet of ESP32 sensor modules deployed across industrial zones:

| Sensor ID | Location | Channel / Type |
|---|---|---|
| `AA:BB:CC:DD:EE:11` to `13` | Hydroponics Farm, Zones 1 to 3 | Soil Moisture (%) |
| `AA:BB:CC:DD:EE:21` to `23` | Power Substation 1-3 & Office Meter | Power Consumption (W) |
| `AA:BB:CC:DD:EE:31` and `32` | Greenhouse Facilities A & B | Ambient Temperature (°C) |

### Interactive Simulator Controls
| Key | Action | Data Structure Impact |
|---|---|---|
| **S** | Injects an acute **power surge** (W > 3000) | Triggers `PriorityQueue` critical path & deduplicated alert in `HashSet` |
| **M** | Injects an acute **moisture drop** (% < 20) | Triggers safety critical alert & exercises `SuggestionEngine` patterns |
| **D** | Forces a random node into a **silent/disconnected** state | Disconnect monitor detects silence, triggering graph detour recalculations |
| **R** | **Reconnects** all offline nodes | Clears alerts and restores regular Prim MST routing paths |
| **B** | Transmits an asynchronous **burst of 300 packets** | Demonstrates FIFO and Priority queues draining under load |
| **Q** | Terminates simulator execution | Clean shutdown |

---

## Troubleshooting

- **Port Conflict (`Address already in use: 7008 / 7142`)**:
  ```powershell
  Get-NetTCPConnection -LocalPort 7008
  Stop-Process -Id <PID> -Force
  ```
- **Untrusted Localhost SSL Certificate**:
  ```powershell
  dotnet dev-certs https --trust
  ```
- **Clear & Rebuild Workspace**:
  ```powershell
  dotnet clean
  dotnet build
  ```
