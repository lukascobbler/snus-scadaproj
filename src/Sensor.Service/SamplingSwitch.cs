namespace Sensor.Service;

public interface ISamplingSwitch { bool Enabled { get; set; } }
public class SamplingSwitch : ISamplingSwitch { public bool Enabled { get; set; } = true; }
