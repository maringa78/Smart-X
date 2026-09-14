namespace SmartX.API.Models
{
    public enum SensorCategory
    {
        Environmental,
        PowerConsumption,
        Activator

    }
    public class  SensorRecord
    {
        public string MacAddress { get; set; } = string.Empty;

        public string DeploymentLocation { get; set; } = string.Empty;

        public SensorCategory Category { get; set; }

        public List<string> AttachedFiles { get; set; } = new ();
    }
}
