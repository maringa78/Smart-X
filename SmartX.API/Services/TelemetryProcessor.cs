namespace SmartX.API.Services
{
    /// <summary>
    /// Background worker that drains the TelemetryPipeline and stores each packet.
    /// Because it always asks the pipeline for the next packet, critical alerts
    /// are processed before anything waiting in the standard queue.
    /// </summary>
    public class TelemetryProcessor(
        TelemetryPipeline pipeline,
        SensorRegistry registry,
        ILogger<TelemetryProcessor> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (pipeline.TryDequeue(out var packet, out var wasCritical) && packet is not null)
                {
                    registry.RecordTelemetry(packet);

                    if (wasCritical)
                        logger.LogWarning("CRITICAL packet processed: {Packet}", packet);
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