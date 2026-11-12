using System.Net;
using System.Net.Http.Json;
using SmartX.Client.Models;

namespace SmartX.Client.Services;

public class SmartXApiClient(HttpClient http)
{
    public async Task<List<SensorRecord>> GetSensorsAsync() =>
        await http.GetFromJsonAsync<List<SensorRecord>>("/api/sensors") ?? new();

    public async Task RegisterSensorAsync(SensorRecord sensor) =>
        await http.PostAsJsonAsync("/api/sensors", sensor);

    public async Task<List<TelemetryPacket>> GetTelemetryAsync(string sensorId) =>
        await http.GetFromJsonAsync<List<TelemetryPacket>>($"/api/telemetry/{sensorId}") ?? new();

    public async Task SubmitTelemetryAsync(TelemetryPacket packet) =>
        await http.PostAsJsonAsync("/api/telemetry", packet);

    public async Task<bool> UploadFileAsync(string mac, Stream fileStream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", fileName);

        var response = await http.PostAsync($"/api/sensors/{mac}/upload", content);
        return response.IsSuccessStatusCode;
    }

    // ----- Real-Time Command Stream -----

    public async Task<PipelineStatus> GetPipelineStatusAsync() =>
        await http.GetFromJsonAsync<PipelineStatus>("/api/pipeline/status") ?? new();

    public async Task<AlertsResponse> GetAlertsAsync() =>
        await http.GetFromJsonAsync<AlertsResponse>("/api/alerts") ?? new();

    /// <summary>
    /// Asks the API to filter devices by text (device ID or location), category and status.
    /// logSearch should be true only when the user deliberately submits a search (Enter or
    /// the Search button), so the suggestion engine learns from real searches and not from
    /// every keystroke or background refresh.
    /// </summary>
    public async Task<List<SensorRecord>> SearchDevicesAsync(
        string? query, SensorCategory? category, DeviceStatus? status, bool logSearch = false)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(query)) parts.Add($"q={Uri.EscapeDataString(query.Trim())}");
        if (category is not null) parts.Add($"category={category}");
        if (status is not null) parts.Add($"status={status}");
        if (logSearch) parts.Add("log=true");

        var url = "/api/devices/search" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
        return await http.GetFromJsonAsync<List<SensorRecord>>(url) ?? new();
    }

    // ----- Manual overrides (stack-based history with undo) -----

    public async Task<CommandRecord?> IssueCommandAsync(string deviceId, string name, string state)
    {
        var response = await http.PostAsJsonAsync("/api/commands", new { deviceId, name, state });
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CommandRecord>()
            : null;
    }

    /// <summary>Pops the newest command off the API's stack. Returns null if there is nothing to undo.</summary>
    public async Task<CommandRecord?> UndoCommandAsync()
    {
        var response = await http.PostAsync("/api/commands/undo", null);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CommandRecord>()
            : null;
    }

    public async Task<List<CommandRecord>> GetCommandHistoryAsync() =>
        await http.GetFromJsonAsync<List<CommandRecord>>("/api/commands/history") ?? new();

    // ----- Predictive suggestions -----

    public async Task<SuggestionResult> GetSuggestionsAsync() =>
        await http.GetFromJsonAsync<SuggestionResult>("/api/suggestions") ?? new();

    /// <summary>Fills the engine with simulated operator history so the patterns can be demonstrated.</summary>
    public async Task SeedDemoHistoryAsync() =>
        await http.PostAsync("/api/suggestions/demo-history", null);

    // ----- Network Topology and Mesh Routing -----

    public async Task<MeshResponse> GetMeshAsync() =>
        await http.GetFromJsonAsync<MeshResponse>("/api/mesh") ?? new();

    public async Task<NetworkHealthDto> GetMeshHealthAsync() =>
        await http.GetFromJsonAsync<NetworkHealthDto>("/api/mesh/health") ?? new();

    public async Task<SpanningTreeDto> GetMstAsync() =>
        await http.GetFromJsonAsync<SpanningTreeDto>("/api/mesh/mst") ?? new();

    public Task<RouteInfoDto?> GetRouteAsync(string nodeId) =>
        GetOrNullAsync<RouteInfoDto>($"/api/mesh/route/{Uri.EscapeDataString(nodeId)}");

    public Task<MstRouteDto?> GetMstRouteAsync(string nodeId) =>
        GetOrNullAsync<MstRouteDto>($"/api/mesh/mst/route/{Uri.EscapeDataString(nodeId)}");

    /// <summary>Marks a node as failed (down) or working again (up) in the mesh.</summary>
    public async Task SetNodeStateAsync(string nodeId, bool down) =>
        await http.PostAsync($"/api/mesh/nodes/{Uri.EscapeDataString(nodeId)}/{(down ? "down" : "up")}", null);

    /// <summary>Readings for one sensor in a time range, answered by the API's AVL tree.</summary>
    public async Task<ArchiveResponse> GetArchiveAsync(string sensorId, DateTime? start, DateTime? end)
    {
        var query = new List<string>();
        if (start is not null)
            query.Add($"from={Uri.EscapeDataString(start.Value.ToUniversalTime().ToString("o"))}");
        if (end is not null)
            query.Add($"to={Uri.EscapeDataString(end.Value.ToUniversalTime().ToString("o"))}");

        var url = $"/api/archive/{Uri.EscapeDataString(sensorId)}";
        if (query.Count > 0) url += "?" + string.Join("&", query);

        return await http.GetFromJsonAsync<ArchiveResponse>(url) ?? new();
    }

    // Returns null for a 404 instead of throwing (for example a node with no route).
    private async Task<T?> GetOrNullAsync<T>(string url) where T : class
    {
        var response = await http.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }
}