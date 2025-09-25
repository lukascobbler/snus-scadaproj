using Microsoft.EntityFrameworkCore;
using Sensor.Service.Data;
using Shared.Contracts;

namespace Sensor.Service;

public class SensorService : Shared.Contracts.ISensorService
{
    private readonly string _sensorId;
    private readonly SensorDbContext _db;
    private readonly ISamplingSwitch _switch;

    public SensorService(string sensorId, SensorDbContext db, ISamplingSwitch samplingSwitch)
    {
        _sensorId = sensorId;
        _db = db;
        _switch = samplingSwitch;
    }

    public void Start() => _switch.Enabled = true;
    public void Stop() => _switch.Enabled = false;

    public double GetLatest()
    {
        var latest = _db.Readings
            .OrderByDescending(r => r.Timestamp)
            .Select(r => (double?)r.TemperatureC)
            .FirstOrDefault();
        return latest ?? 20.0;
    }

    public SensorSnapshot GetSnapshot(TimeSpan lookback)
    {
        var now = DateTimeOffset.UtcNow;
        var from = now - lookback;
        var vals = _db.Readings
            .Where(r => r.Timestamp >= from && r.Timestamp <= now)
            .OrderBy(r => r.Timestamp)
            .Select(r => r.TemperatureC)
            .ToArray();

        return new SensorSnapshot
        {
            SensorId = _sensorId,
            From = from,
            To = now,
            Values = vals
        };
    }

    public void AppendReconciled(double value)
    {
        _db.Readings.Add(new SensorReading
        {
            SensorId = _sensorId,
            Timestamp = DateTimeOffset.UtcNow,
            TemperatureC = value,
            IsReconciled = true
        });
        _db.SaveChanges();
    }
}
