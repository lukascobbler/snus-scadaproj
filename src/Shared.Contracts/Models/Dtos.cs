using System.Runtime.Serialization;

namespace Shared.Contracts;

[DataContract]
public class SensorSnapshot
{
    [DataMember(Order = 1)] public string SensorId { get; set; } = "";
    [DataMember(Order = 2)] public DateTimeOffset From { get; set; }
    [DataMember(Order = 3)] public DateTimeOffset To { get; set; }
    [DataMember(Order = 4)] public double[] Values { get; set; } = Array.Empty<double>();
    public SensorSnapshot() { }
}
