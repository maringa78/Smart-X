using SmartX.Api.Models;

namespace SmartX.API.Models
{
    public class TelemetryHistoryStore
    {
        private double[][] _rawBatches = Array.Empty<double[]>();
        private readonly List<TelemetryPacket<double>> _processed = new();

        public void AddBatch(double[] batch)
        { 
            var grown = new double[_rawBatches.Length + 1][];
            Array.Copy(_rawBatches, grown, _rawBatches.Length);
            grown[^1] = batch;
            _rawBatches = grown;

        }
        public void FLushToList(string sensorId, string unit)
        {
            foreach (var batch in _rawBatches)
            {
                foreach (var reading in batch)
                {
                    _processed.Add(new TelemetryPacket<double>(sensorId, reading, unit));
                }
            }
            _rawBatches = Array.Empty<double[]>(); // cleared after flushing
        }
        public IReadOnlyList<TelemetryPacket<double>> ProcessedReadings => _processed.AsReadOnly();
    }

    }
