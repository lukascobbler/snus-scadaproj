using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Sensor.Service.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Sensor.Service;

public class SamplerWorker : BackgroundService
{
    private readonly ILogger<SamplerWorker> _log;
    private readonly IServiceProvider _sp;
    private readonly string _sensorId;
    private readonly ISamplingSwitch _switch;
    private readonly Random _rng = new();

    public SamplerWorker(ILogger<SamplerWorker> log, IServiceProvider sp, ISamplingSwitch samplingSwitch, string sensorId)
    {
        _log = log;
        _sp = sp;
        _switch = samplingSwitch;
        _sensorId = sensorId;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = _sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SensorDbContext>();
            await db.Database.EnsureCreatedAsync(stoppingToken);
        }

        _log.LogInformation("{SensorId} sampler started", _sensorId);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromSeconds(_rng.Next(1, 11)); // 1-10s
            await Task.Delay(delay, stoppingToken);

            if (!_switch.Enabled) continue;

            var value = NextTemp();
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SensorDbContext>();
            db.Readings.Add(new SensorReading
            {
                SensorId = _sensorId,
                Timestamp = DateTimeOffset.UtcNow,
                TemperatureC = value,
                IsReconciled = false
            });
            await db.SaveChangesAsync(stoppingToken);
            _log.LogInformation("{SensorId} wrote {Value:F2} °C", _sensorId, value);
        }
    }

    private double NextTemp()
    {
        double delta = (_rng.NextDouble() - 0.5) * 0.8;
        return 20.0 + delta;
    }
}
