namespace SmartX.API.Services
{
    /// <summary>
    /// Background worker that drains the TelemetryPipeline and stores each packet.
    /// Because it always asks the pipeline for the next packet, critical alerts
    /// are processed before anything waiting in the standard queue.
    /// It also keeps the AlertTracker and the sorted history up to date.
    /// </summary>
    public class TelemetryProcessor(
        TelemetryPipeline pipeline,
        SensorRegistry registry,
        AlertTracker alerts,
        SuggestionEngine suggestions,
        TelemetryHistory history,
        TelemetryArchive archive,
        ILogger<TelemetryProcessor> logger) : BackgroundService
    {
        // Models the time real processing takes per packet (e.g. a database write).
        // It lets the queues build up visibly during a burst. Set to 0 to disable.
        private const int SimulatedWorkMs = 5;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (pipeline.TryDequeue(out var packet, out var wasCritical) && packet is not null)
                {
                    var knownDevice = registry.RecordTelemetry(packet);
                    history.Add(packet);
                    archive.Add(packet);

                    if (!knownDevice)
                        logger.LogWarning("Telemetry received from unregistered device {SensorId}", packet.SensorId);

                    // The device is talking again, so any disconnect alert is resolved.
                    if (alerts.Clear(packet.SensorId, AlertType.Disconnected))
                        logger.LogInformation("Device {SensorId} reconnected", packet.SensorId);

                    if (wasCritical)
                    {
                        // Only the first critical reading raises a new alert; repeats are ignored by the set.
                        if (alerts.Raise(packet.SensorId, AlertType.CriticalReading))
                        {
                            logger.LogWarning("NEW CRITICAL alert: {Packet}", packet);
                            suggestions.RecordAlert(packet.SensorId, AlertType.CriticalReading);
                        }
                    }
                    else
                    {
                        alerts.Clear(packet.SensorId, AlertType.CriticalReading);
                    }

                    if (SimulatedWorkMs > 0)
                        await Task.Delay(SimulatedWorkMs, stoppingToken);
                }
                else
                {
                    // Nothing waiting: pause briefly instead of spinning the CPU.
                    await Task.Delay(50, stoppingToken);
                }
            }
        }
    }
}