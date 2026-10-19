# Smart-X Gateway

An IoT gateway and management dashboard for monitoring distributed sensor networks (hydroponics farms, smart-grid meters, greenhouse sensors). Devices send telemetry to a .NET API, which queues, validates, stores and analyses it, and a Blazor dashboard shows the live state of the network.

- GitHub: https://github.com/EMWCCN/PROG7312_POE_Gr2_St10287116.git
- YouTube demo (Part 1): https://www.youtube.com/watch?v=62TH5MTy8qI
- YouTube demo (Part 2): https://youtu.be/WAZFYqw265o

## Contents

1. [Project overview](#project-overview)
2. [Prerequisites](#prerequisites)
3. [Project structure](#project-structure)
4. [Setup and running](#setup-and-running)
5. [Using the application](#using-the-application)
6. [Data structures and how to test them](#data-structures-and-how-to-test-them)
7. [API endpoints](#api-endpoints)
8. [Simulator reference](#simulator-reference)
9. [Troubleshooting](#troubleshooting)
10. [Notes and limitations](#notes-and-limitations)

---

## Project overview

Smart-X is made of three projects:

| Project | Type | Purpose |
|---|---|---|
| `SmartX.API` | ASP.NET Core 10 minimal API | Receives telemetry, runs the processing pipeline, stores device state, history, alerts and commands, and generates suggestions |
| `SmartX.Client` | Blazor Server (.NET 10) | The dashboard. Talks to the API asynchronously over HTTP |
| `SmartX.Simulator` | .NET 10 console app | Acts like a fleet of ESP32 devices. Registers devices and sends telemetry to the API over HTTP, exactly as real hardware would |

All data is held **in memory**. There is no database, and everything resets when the API restarts.

### Features

**Part 1: ingestion and validation gateway**

- Sensor registration (MAC address, deployment location, category)
- Telemetry ingestion with colour-coded severity
- File upload (logs, photos) to a sensor profile
- Landing page with the three architectural pillars

**Part 2: real-time processing and device management**

- Navigation between all active pillars, with session state kept when switching pages
- Asynchronous ingestion pipeline: FIFO queue plus a priority queue for critical alerts
- Live device registry with O(1) lookup by MAC address
- Time-ordered telemetry history per sensor
- Duplicate-free alert tracking and automatic offline detection
- Manual override commands with a stack-based undo history
- Predictive suggestion engine ("Suggested Actions" and "Automated Insights") that learns from alerts, searches and commands
- Device search by text, category and status
- Live Command Stream dashboard (device table, status badges, queue and alert counters, refreshed every 2 seconds)
- Device simulator with controls to trigger spikes, drops, disconnects and bursts

---

## Prerequisites

- **.NET SDK 10.0 or higher**: https://dotnet.microsoft.com/download (check with `dotnet --version`)
- **Visual Studio 2022 or later** (recommended), or VS Code with the C# extension
- **Git**, to clone the repository
- A trusted HTTPS development certificate. If your browser or the simulator complains about the certificate, run:

```bash
dotnet dev-certs https --trust
```

---

## Project structure

```
SmartX/
├── SmartX.API/
│   ├── Program.cs                    # Startup, service registration, all endpoints
│   ├── SensorRegistry.cs             # Dictionary of devices keyed by MAC address
│   ├── Models/
│   │   ├── SensorRecord.cs           # Device record plus live status fields
│   │   ├── TelemetryPacket.cs        # Generic telemetry wrapper
│   │   ├── PowerReading.cs           # Power value with operator overloads
│   │   ├── TelemetryHistoryStore.cs  # Jagged-array history buffer
│   │   ├── DeploymentValidator.cs    # Recursive deployment-path validator
│   │   └── Command.cs                # Manual override command and request
│   └── Services/
│       ├── TelemetryPipeline.cs      # Queue + PriorityQueue
│       ├── TelemetryProcessor.cs     # Background worker that drains the pipeline
│       ├── CommandHistory.cs         # Stack of override commands (undo)
│       ├── TelemetryHistory.cs       # SortedDictionary timelines per sensor
│       ├── AlertTracker.cs           # HashSet of active alerts
│       ├── DeviceMonitor.cs          # Background worker that detects offline devices
│       └── SuggestionEngine.cs       # Pattern analysis and suggestions
│
├── SmartX.Client/
│   ├── Program.cs                    # App setup, API client, AppState registration
│   ├── Components/
│   │   ├── App.razor                 # Root component (whole app is interactive)
│   │   ├── Layout/MainLayout.razor   # Top navigation bar
│   │   └── Pages/
│   │       ├── Home.razor            # Dashboard with the three pillars
│   │       ├── Sensors.razor         # Sensor registration, telemetry, uploads
│   │       └── Commands.razor        # Real-Time Command Stream page
│   ├── Services/
│   │   ├── SmartXApiClient.cs        # All API calls
│   │   └── AppState.cs               # Session state shared by every page
│   ├── Models/SmartXModels.cs        # Client-side models
│   └── wwwroot/app.css               # Styling
│
├── SmartX.Simulator/
│   └── Program.cs                    # Device simulator console app
│
└── README.md
```

---

## Setup and running

### 1. Get the code

```bash
git clone https://github.com/EMWCCN/PROG7312_POE_Gr2_St10287116.git
cd PROG7312_POE_Gr2_St10287116
```

### 2. Restore and build

From the solution folder (the one containing the `.sln` file):

```bash
dotnet restore
dotnet build
```

The build should end with `Build succeeded` and `0 Error(s)`.

### 3. Start the API (terminal 1)

```bash
cd SmartX.API
dotnet run
```

The API listens on `https://localhost:7008` (and `http://localhost:5043`). Check it by opening **https://localhost:7008/swagger**.

### 4. Start the client (terminal 2)

```bash
cd SmartX.Client
dotnet run
```

Open the address shown in the terminal (by default `https://localhost:7120`).

The client finds the API through the `ApiBaseUrl` setting in `SmartX.Client/appsettings.json`. If it is missing, it defaults to `https://localhost:7008`. If your API runs on a different port, set it there:

```json
{
  "ApiBaseUrl": "https://localhost:7008"
}
```

### 5. Start the simulator (terminal 3)

Start the API first, then:

```bash
cd SmartX.Simulator
dotnet run
```

To point it at a different address, pass it as an argument: `dotnet run -- http://localhost:5043`.

The simulator registers 8 devices and sends a reading from each every 2 seconds. In Visual Studio you can also right-click each project and choose **Debug → Start New Instance**, or configure multiple startup projects.

> Always start the API **before** the client and the simulator. Restarting the API clears all data, so restart the simulator afterwards to re-register its devices.

---

## Using the application

### Navigation

The top bar has the three architectural pillars:

| Pillar | Status |
|---|---|
| Sensor Data Ingestion and Telemetry | Active |
| Real-Time Command Stream and History | Active |
| Network Topology and Mesh Routing | Disabled until the final PoE |

Pages keep their state when you switch between them. For example, a half-filled registration form is still there when you return to the Sensors page. This works because all pages share one `AppState` service and the whole app runs in a single interactive session. Reloading the browser tab starts a new session and clears that state.

### Sensor Data Ingestion and Telemetry (`/sensors`)

1. **Register a sensor**: MAC address (for example `AA:BB:CC:00:11:22`), deployment location, and category (Environmental, PowerConsumption, Actuator).
2. **Connected sensors**: a table of registered devices, with an upload button for logs and photos.
3. **Live telemetry stream**: readings with colour-coded severity. "Simulate reading" sends a test reading for a registered sensor.

### Real-Time Command Stream and History (`/commands`)

- Stat cards: total devices, online, in alert, offline, packets waiting in the queues (normal / critical), and duplicate alerts blocked.
- A live table of every device with a status badge (Online, Alert, Offline, Awaiting data), its last reading, when it was last seen, and its active alerts.
- Devices that need attention are listed first.
- The page refreshes every 2 seconds (polling) and stops when you leave it.

---

## Data structures and how to test them

Every test below can be run in **Swagger** (`https://localhost:7008/swagger`): expand an endpoint, click **Try it out**, paste the body, and click **Execute**. Stop the simulator (**Q**) first so it does not overwrite your test values.

Category numbers used in request bodies: `0` Environmental, `1` PowerConsumption, `2` Actuator.

### Summary

| Structure | Class | Role |
|---|---|---|
| `Queue<T>` | `TelemetryPipeline` | Normal telemetry, first-in first-out |
| `PriorityQueue<T, TPriority>` | `TelemetryPipeline` | Critical alerts, always processed first |
| `Stack<T>` | `CommandHistory` | Override commands, last-in first-out undo |
| `Dictionary<TKey, TValue>` | `SensorRegistry` | Live devices keyed by MAC address, O(1) lookup |
| `SortedDictionary<TKey, TValue>` | `TelemetryHistory` | Readings per sensor ordered by timestamp |
| `HashSet<T>` | `AlertTracker` | Unique active alerts, blocks duplicates |
| `Queue<T>` + nested `Dictionary` + `PriorityQueue` | `SuggestionEngine` | Sliding event window, pattern table, ranking |

### Queue and priority queue (`TelemetryPipeline`)

`POST /api/telemetry` does not process the packet. It adds it to the pipeline and returns `202 Accepted`. A background worker (`TelemetryProcessor`) removes packets one at a time and always takes from the priority queue first.

A packet is **critical** when its unit is `W` and its value is above `3000`, or its unit is `%` and its value is below `20`.

**Simulate a high-priority alert**

1. Register a sensor: `POST /api/sensors` with
   ```json
   { "macAddress": "AA:BB:CC:DD:EE:01", "deploymentLocation": "Zone 1", "category": 1 }
   ```
2. Send a critical reading: `POST /api/telemetry` with
   ```json
   { "sensorId": "AA:BB:CC:DD:EE:01", "value": 5000, "unit": "W" }
   ```
   The response is `202` with `"wasCritical": true`, and the API console logs `NEW CRITICAL alert`.
3. Send a normal reading:
   ```json
   { "sensorId": "AA:BB:CC:DD:EE:01", "value": 1200, "unit": "W" }
   ```
   The response shows `"wasCritical": false`.
4. Check `GET /api/sensors/AA:BB:CC:DD:EE:01`: `status` is `1` (Online) after the normal reading and `2` (Alert) after the critical one.

**Watch the queues fill (simulator burst)**

Run the simulator and press **B**. It sends 300 packets at once, and every tenth is critical. The simulator prints the queue counts as they drain, and the critical queue empties first. The worker has a small simulated processing delay (`SimulatedWorkMs = 5` in `TelemetryProcessor.cs`) so the queues stay visible. It models per-packet work such as a database write. Set it to `0` to remove it.

`GET /api/pipeline/status` returns the current queue sizes.

### Stack and undo (`CommandHistory`)

Each manual override is pushed onto a stack and remembers the state it replaced. Undo pops the newest command and restores that state.

1. `POST /api/commands` with
   ```json
   { "deviceId": "AA:BB:CC:DD:EE:01", "name": "WaterPump", "state": "On" }
   ```
   The response shows `"previousState": "Off"`.
2. Send the same command with `"state": "Off"`. The response shows `"previousState": "On"`.
3. `GET /api/commands/history` lists both, newest first.
4. `POST /api/commands/undo` removes the newest command and returns it. Check the history again: only the first command remains.
5. Call undo until the stack is empty: the next call returns `404 No commands to undo`.
6. A `state` other than `On` or `Off` returns `400`.

### Dictionary (`SensorRegistry`)

Registered devices are stored in a `Dictionary` keyed by MAC address. When a packet arrives, the processor finds its device in O(1) time and updates its live fields (`status`, `lastSeen`, `lastValue`, `lastUnit`).

1. Register `AA:BB:CC:DD:EE:02` (category `1`).
2. `GET /api/sensors/AA:BB:CC:DD:EE:02` shows `"status": 0` (Registered) and `lastSeen: null`.
3. Send `{ "sensorId": "AA:BB:CC:DD:EE:02", "value": 1200, "unit": "W" }` and repeat step 2. The status is now `1` and the last value and time are filled in.
4. A packet from an unregistered ID is still stored, and the API console logs a warning.
5. `GET /api/devices/search?q=zone` finds devices by MAC or location. You can add `category=1` and/or `status=2`.

### Sorted dictionary (`TelemetryHistory`)

Each sensor has a `SortedDictionary` keyed by timestamp, so readings are always in time order. Use an unregistered ID so old timestamps do not trigger offline detection.

1. Send three packets, in this order, to `POST /api/telemetry` (same body each time except the timestamp and value):
   ```json
   { "sensorId": "HISTORY-TEST-01", "timestamp": "2026-10-18T10:00:00Z", "value": 30, "unit": "%" }
   { "sensorId": "HISTORY-TEST-01", "timestamp": "2026-10-18T08:00:00Z", "value": 40, "unit": "%" }
   { "sensorId": "HISTORY-TEST-01", "timestamp": "2026-10-18T09:00:00Z", "value": 50, "unit": "%" }
   ```
2. `GET /api/history/HISTORY-TEST-01` returns them as **08:00, 09:00, 10:00**, even though they were sent out of order.
3. Optional query values: `from`, `to` (ISO timestamps) and `limit` (the most recent N). For example `?from=2026-10-18T08:30:00Z` returns the last two.

A sorted dictionary cannot jump straight to a time range, so `from` and `to` scan the timeline. The tree structures planned for the final PoE address this.

### HashSet and duplicate alerts (`AlertTracker`, `DeviceMonitor`)

Active alerts are stored in a `HashSet`. An alert is a device plus a problem (`CriticalReading` or `Disconnected`). `HashSet.Add` returns `false` when the alert is already active, so repeats are ignored and counted.

**Duplicate critical alerts**

1. Register `AA:BB:CC:DD:EE:03` and send one normal packet.
2. Send three critical packets (for example `5000` `W`).
3. `GET /api/alerts` shows **one** `CriticalReading` alert, and `suppressedDuplicates` is `2`.
4. Send a normal packet. The alert clears.

**Disconnect detection**

A background monitor checks every 5 seconds. A device that has reported before but has been silent for 20 seconds is marked Offline, and one `Disconnected` alert is raised.

1. With the simulator running, press **D**. One device goes silent.
2. After about 20 seconds the device shows **Offline** on the Command Stream page, and `GET /api/alerts` lists a `Disconnected` alert for it. The API console logs one warning, not one per check.
3. Press **R**. The device reconnects, the alert clears and it returns to Online.

### Suggestion engine (`SuggestionEngine`)

The engine learns from alerts, searches and commands:

1. Every alert, search and command is an event. A `Queue` holds a sliding 3-minute window of recent events.
2. Each new event is paired with the distinct events before it in the window. The counts go into a `Dictionary<string, Dictionary<string, int>>`, so the engine knows, for example, that a moisture alert on one sensor was followed by a search for the pump 4 times.
3. To suggest, it looks at what is happening now (active alerts and the latest action), finds what usually followed, and ranks the candidates with a `PriorityQueue` by confidence. A pattern must be seen at least twice.
4. **Automated Insights** list devices that are offline, in alert, or within 10% of the power limit or 5 points of the moisture limit.

**Test it with simulated operator history**

1. Run the simulator for a few seconds so devices are registered, then press **Q**.
2. `POST /api/suggestions/demo-history` adds simulated operator history (a moisture drop on `AA:BB:CC:DD:EE:11` followed by searching "pump a" and switching `WaterPump` on, and a power spike on `AA:BB:CC:DD:EE:21` followed by switching `Relay1` off).
3. Trigger the moisture alert:
   ```json
   { "sensorId": "AA:BB:CC:DD:EE:11", "value": 10, "unit": "%" }
   ```
4. `GET /api/suggestions` returns suggested actions ("Search for pump a", "Set WaterPump On on AA:BB:CC:DD:EE:41"), each with a confidence and a reason, plus an insight that device `EE:11` is in alert.
5. Issue the suggested command with `POST /api/commands`, then request suggestions again. That command is no longer suggested, because it has already happened.
6. Real searches also teach the engine: `GET /api/devices/search?q=pump&log=true` records the search.

---

## API endpoints

### Sensors and telemetry

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/sensors` | All registered sensors with live status |
| POST | `/api/sensors` | Register a sensor (re-registering a MAC updates its details) |
| GET | `/api/sensors/{mac}` | One sensor by MAC address |
| POST | `/api/sensors/{mac}/upload` | Upload a file to a sensor profile |
| POST | `/api/telemetry` | Submit a reading (queued, returns 202) |
| GET | `/api/telemetry/{sensorId}` | Stored readings for a sensor |
| GET | `/api/history/{sensorId}` | Time-ordered readings (`from`, `to`, `limit`) |
| GET | `/api/pipeline/status` | Packets waiting in the normal and critical queues |

### Commands, alerts and suggestions

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/commands` | Issue a manual override (`deviceId`, `name`, `state`) |
| POST | `/api/commands/undo` | Undo the most recent command |
| GET | `/api/commands/history` | Command history, newest first |
| GET | `/api/alerts` | Active alerts and the duplicate-blocked count |
| GET | `/api/devices/search` | Search by `q`, `category`, `status` (add `log=true` to teach the engine) |
| GET | `/api/suggestions` | Suggested actions and automated insights |
| POST | `/api/suggestions/demo-history` | Add simulated operator history for demos and testing |

---

## Simulator reference

The simulator registers these devices:

| MAC address | Location | Type |
|---|---|---|
| `AA:BB:CC:DD:EE:11` to `13` | Hydroponics Farm, Zones 1 to 3 | Moisture (%) |
| `AA:BB:CC:DD:EE:21` to `23` | Smart Grid substations and office meter | Power (W) |
| `AA:BB:CC:DD:EE:31` and `32` | Greenhouse rooms 1 and 2 | Temperature (C) |

| Key | Action |
|---|---|
| **S** | Power spike on a random power meter for its next 3 readings |
| **M** | Moisture drop on a random moisture sensor for its next 3 readings |
| **D** | A random device goes silent |
| **R** | All devices reconnect |
| **B** | Burst of 300 packets (every tenth is critical) |
| **Q** | Quit |

Each line the simulator prints shows packets sent, failed requests, devices online and the current queue sizes.

---

## Troubleshooting

**Port already in use** (`Address already in use: https://localhost:7008`)

```powershell
Get-NetTCPConnection -LocalPort 7008
Stop-Process -Id <PID> -Force
```

Or change the ports in `Properties/launchSettings.json` and update `ApiBaseUrl` in the client.

**Client cannot reach the API** (`HttpRequestException`, or "Cannot reach the API" on the Command Stream page)

1. Check the API terminal shows `Now listening on: https://localhost:7008`.
2. Check `ApiBaseUrl` in the client matches the API address.
3. Run `dotnet dev-certs https --trust` and restart.

**Simulator cannot reach the API**: start `SmartX.API` first, then run the simulator again.

**Simulate reading says "Register a sensor first"**: the API restarts with no data. Register a sensor, or run the simulator.

**Styles look wrong**: hard refresh with Ctrl+F5.

**Build errors**:

```bash
dotnet clean
dotnet restore
dotnet build
```

and check `dotnet --version` is 10.0 or higher.

---

## Notes and limitations

- All data is stored in memory and is lost when the API restarts. The suggestion engine's learned patterns reset too.
- The dashboard refreshes by polling the API (every 2 seconds on the Command Stream page), not by server push.
- `SortedDictionary` history supports ordered retrieval, but time-range filters scan the timeline. Tree-based indexing is planned for the final PoE.
- The simulator trusts the local development certificate for localhost only. This is for development use.
- Network Topology and Mesh Routing is disabled until the final PoE.
- This is a development build. A production deployment would need authentication, restricted CORS, rate limiting and persistent storage.
