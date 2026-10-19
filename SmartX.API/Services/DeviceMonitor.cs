using SmartX.API.Models;

namespace SmartX.API.Services
{
    /// <summary>
    /// Background worker that checks every few seconds for devices that have gone
    /// quiet. A device that has reported before but not within the timeout is
    /// marked Offline and a Disconnected alert is raised for it. The same offline
    /// device is found on every check, but the HashSet lets only the first one through.
    /// </summary>
    public class DeviceMonitor(
        SensorRegistry registry,
        AlertTracker alerts,
        SuggestionEngine suggestions,
        ILogger<DeviceMonitor> logger) : BackgroundService
    {
        // Short values so the behaviour is easy to show in a demo.
        private static readonly TimeSpan OfflineAfter = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan CheckEvery = TimeSpan.FromSeconds(5);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var mac in registry.GetStaleDevices(OfflineAfter))
                {
                    if (alerts.Raise(mac, AlertType.Disconnected))
                    {
                        registry.SetStatus(mac, DeviceStatus.Offline);
                        suggestions.RecordAlert(mac, AlertType.Disconnected);
                        logger.LogWarning("Device {Mac} has disconnected (no data for {Seconds}s)",
                            mac, OfflineAfter.TotalSeconds);
                    }
                }

                await Task.Delay(CheckEvery, stoppingToken);
            }
        }
    }
}