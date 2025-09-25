using System;
using System.Linq;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

public class CoordinatorService : ICoordinatorService
{
    private readonly ILogger<CoordinatorService> _log;
    private readonly ChannelFactory<ISensorService>[] _sensors;

    private readonly SemaphoreSlim _mutex = new(1, 1);
    private volatile bool _inProgress;

    public CoordinatorService(
        ILogger<CoordinatorService> log,
        IEnumerable<ChannelFactory<ISensorService>> sensors)
    {
        _log = log;
        _sensors = sensors?.ToArray() ?? Array.Empty<ChannelFactory<ISensorService>>();
        if (_sensors.Length < 3)
            throw new InvalidOperationException(
                $"Expected 3 sensor channel factories, got {_sensors.Length}. " +
                "Check Program.cs registrations for CreateSensor(...).");
    }

    public bool IsReconInProgress() => _inProgress;

    public async Task<ReconResult> ReconcileAsync()
    {
        await _mutex.WaitAsync();
        _inProgress = true;
        try
        {
            var ch1 = _sensors[0].CreateChannel();
            var ch2 = _sensors[1].CreateChannel();
            var ch3 = _sensors[2].CreateChannel();

            var x1 = ch1.GetLatest();
            var x2 = ch2.GetLatest();
            var x3 = ch3.GetLatest();
            var avg = (x1 + x2 + x3) / 3.0;

            ch1.AppendReconciled(avg);
            ch2.AppendReconciled(avg);
            ch3.AppendReconciled(avg);

            ((IClientChannel)ch1).Close();
            ((IClientChannel)ch2).Close();
            ((IClientChannel)ch3).Close();

            return new ReconResult(true, DateTimeOffset.UtcNow, avg, "Reconciled to average of latests.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Reconcile failed");
            return new ReconResult(false, DateTimeOffset.UtcNow, double.NaN, $"Reconcile failed: {ex.Message}");
        }
        finally
        {
            _inProgress = false;
            _mutex.Release();
        }
    }
}
