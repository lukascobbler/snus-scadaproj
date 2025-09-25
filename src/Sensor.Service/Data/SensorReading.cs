using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensor.Service.Data;

[Table("SensorReadings")]
public class SensorReading
{
    [Key] public int Id { get; set; }
    [Required, MaxLength(8)] public string SensorId { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public double TemperatureC { get; set; }
    public bool IsReconciled { get; set; }
}
