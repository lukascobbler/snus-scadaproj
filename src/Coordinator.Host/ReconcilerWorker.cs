using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

public class ReconcilerWorker : BackgroundService
{
    private readonly ILogger<ReconcilerWorker> _log;
    private readonly CoordinatorService _coord;

    public ReconcilerWorker(ILogger<ReconcilerWorker> log, CoordinatorService coord)
    { _log = log; _coord = coord; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var period = TimeSpan.FromMinutes(1);
        _log.LogInformation("ReconcilerWorker started; period={Period}", period);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(period, ct);
                if (ct.IsCancellationRequested) break;

                _log.LogInformation("Scheduled reconcile tick…");
                var res = await _coord.ReconcileAsync();
                _log.LogInformation("Scheduled reconcile result: {OK} avg={Avg}", res.Success, res.AveragedValue);
            }
            catch (TaskCanceledException) { /* shutdown */ }
            catch (Exception ex) { _log.LogError(ex, "Scheduled reconcile failed"); }
        }
    }
}
