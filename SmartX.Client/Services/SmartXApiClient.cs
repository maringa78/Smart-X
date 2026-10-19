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
}