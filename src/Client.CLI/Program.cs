using System;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Client.CLI.Contracts;
using Shared.Contracts;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // CLI args (optional)
        // --tol <double>   : quorum tolerance (default 5.0)
        // --interval <ms>  : watch loop interval in ms (default 5000)
        // --watch          : keep running; omit for single-shot
        var tol = GetArg(args, "--tol", 5);
        var intervalMs = (int)GetArg(args, "--interval", 5000);
        var watch = args.Contains("--watch") && intervalMs > 0;

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        var coord = CreateCoordinator();
        var s1 = CreateSensor(Ports.S1);
        var s2 = CreateSensor(Ports.S2);
        var s3 = CreateSensor(Ports.S3);

        try
        {
            do
            {
                await WaitIfReconInProgress(coord, cts.Token);

                // read latests
                var (x1, x2, x3) = GetLatests(s1, s2, s3);
                var mean = (x1 + x2 + x3) / 3.0;
                var inliers = new[] { x1, x2, x3 }.Where(v => Math.Abs(v - mean) <= tol).ToArray();

                if (inliers.Length >= 2)
                {
                    var accepted = inliers.Average();
                    Console.WriteLine($"Quorum {inliers.Length}/3 → accepted={accepted:F2} (mean={mean:F2}, tol=±{tol})");
                }
                else
                {
                    Console.WriteLine($"No quorum (±{tol}). Triggering reconciliation…");
                    var res = await coord.ReconcileAsync();
                    Console.WriteLine($"Reconcile → Success={res.Success}, Avg={res.AveragedValue:F2}, Msg={res.Message}");

                    await WaitIfReconInProgress(coord, cts.Token);

                    // re-read after reconcile
                    (x1, x2, x3) = GetLatests(s1, s2, s3);
                    mean = (x1 + x2 + x3) / 3.0;
                    Console.WriteLine($"Post-reconcile latests: [{x1:F2}, {x2:F2}, {x3:F2}] mean={mean:F2}");
                }

                if (!watch) break;
                await Task.Delay(intervalMs, cts.Token);
            }
            while (!cts.IsCancellationRequested);

            return 0;
        }
        catch (OperationCanceledException) { return 0; }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal: {ex}");
            return 1;
        }
        finally
        {
            Close(s1); Close(s2); Close(s3); Close(coord);
        }
    }


    static (double x1, double x2, double x3) GetLatests(
        ISensorServiceClient s1, ISensorServiceClient s2, ISensorServiceClient s3)
    {
        var x1 = s1.GetLatest();
        var x2 = s2.GetLatest();
        var x3 = s3.GetLatest();
        Console.WriteLine($"Latests: [{x1:F2}, {x2:F2}, {x3:F2}]");
        return (x1, x2, x3);
    }

    static async Task WaitIfReconInProgress(ICoordinatorServiceClient coord, CancellationToken ct)
    {
        while (coord.IsReconInProgress())
        {
            Console.WriteLine("Reconciliation in progress… waiting…");
            await Task.Delay(250, ct);
        }
    }

    static ISensorServiceClient CreateSensor(int port)
    {
        var binding = new BasicHttpBinding();
        var address = new EndpointAddress($"http://localhost:{port}/sensor");
        var factory = new ChannelFactory<ISensorServiceClient>(binding, address);
        return factory.CreateChannel();
    }

    static ICoordinatorServiceClient CreateCoordinator()
    {
        var binding = new BasicHttpBinding();
        var address = new EndpointAddress($"http://localhost:{Ports.Coordinator}/coord");
        var factory = new ChannelFactory<ICoordinatorServiceClient>(binding, address);
        return factory.CreateChannel();
    }

    static void Close(object ch)
    {
        try { if (ch is IClientChannel cc) cc.Close(); }
        catch { if (ch is IClientChannel cc2) cc2.Abort(); }
    }

    static double GetArg(string[] args, string name, double fallback)
    {
        var i = Array.IndexOf(args, name);
        return (i >= 0 && i + 1 < args.Length && double.TryParse(args[i + 1], out var v)) ? v : fallback;
    }
}
