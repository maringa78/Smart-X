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

    /// Pops the newest command off the API's stack. Returns null if there is nothing to undo.
    public async Task<CommandRecord?> UndoCommandAsync()
    {
        var response = await http.PostAsync("/api/commands/undo", null);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CommandRecord>()
            : null;
    }

    public async Task<List<CommandRecord>> GetCommandHistoryAsync() =>
        await http.GetFromJsonAsync<List<CommandRecord>>("/api/commands/history") ?? new();
}
