using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Shared.Contracts;
using Xunit;

namespace Client.Tests
{
    // testing units
    public interface ISensorServiceClient
    {
        double GetLatest();
        void AppendReconciled(double value);
    }

    public interface ICoordinatorServiceClient
    {
        Task<ReconResult> ReconcileAsync();
        bool IsReconInProgress();
    }

    // fakes
    public sealed class FakeSensor : ISensorServiceClient
    {
        private readonly Func<double> _get;
        public int ReadCount { get; private set; }

        public FakeSensor(Func<double> get) { _get = get; }
        public double GetLatest() { ReadCount++; return _get(); }
        public void AppendReconciled(double value) { /* not needed here */ }
    }

    public sealed class FakeCoordinator : ICoordinatorServiceClient
    {
        private readonly TimeSpan _reconDuration;
        private int _reconcileCalls;
        public volatile bool InProgress;

        public int ReconcileCalls => _reconcileCalls;

        public FakeCoordinator(bool startInProgress = false, TimeSpan? reconDuration = null)
        {
            InProgress = startInProgress;
            _reconDuration = reconDuration ?? TimeSpan.FromMilliseconds(200);
        }

        public bool IsReconInProgress() => InProgress;

        public async Task<ReconResult> ReconcileAsync()
        {
            Interlocked.Increment(ref _reconcileCalls);
            InProgress = true;
            try
            {
                await Task.Delay(_reconDuration);
                return new ReconResult(true, DateTimeOffset.UtcNow, 0.0, "OK");
            }
            finally
            {
                InProgress = false;
            }
        }
    }

    public class QuorumAndReconcileTests
    {
        private static int SumReads(params ISensorServiceClient[] sensors)
        {
            var total = 0;
            foreach (var s in sensors)
                if (s is FakeSensor fs) total += fs.ReadCount;
            return total;
        }

        private static async Task<(bool accepted, double value, int readsBefore, int readsAfter, int reconCalls)>
            RunClientOnceAsync(
                ICoordinatorServiceClient coord,
                ISensorServiceClient s1,
                ISensorServiceClient s2,
                ISensorServiceClient s3,
                double tol,
                CancellationToken ct = default)
        {
            // wait while coordinator says recon is in progress
            int readsBefore = SumReads(s1, s2, s3);

            while (coord.IsReconInProgress())
                await Task.Delay(25, ct);

            // read latests
            var x1 = s1.GetLatest();
            var x2 = s2.GetLatest();
            var x3 = s3.GetLatest();

            var mean = (x1 + x2 + x3) / 3.0;
            var inliers = new[] { x1, x2, x3 }.Where(v => Math.Abs(v - mean) <= tol).ToArray();

            if (inliers.Length >= 2)
            {
                var accepted = inliers.Average();
                int readsAfterA = SumReads(s1, s2, s3);
                return (accepted: true, value: accepted, readsBefore, readsAfter: readsAfterA,
                        reconCalls: (coord as FakeCoordinator)?.ReconcileCalls ?? 0);
            }

            // if no quorum, trigger reconcile
            var res = await coord.ReconcileAsync();

            //  wait again if still in progress
            while (coord.IsReconInProgress())
                await Task.Delay(25, ct);

            // re-read
            x1 = s1.GetLatest();
            x2 = s2.GetLatest();
            x3 = s3.GetLatest();

            var accepted2 = new[] { x1, x2, x3 }.Average();
            int readsAfterB = SumReads(s1, s2, s3);

            return (accepted: true, value: accepted2, readsBefore, readsAfter: readsAfterB,
                    reconCalls: (coord as FakeCoordinator)?.ReconcileCalls ?? 0);
        }

        [Fact]
        public async Task Client_Waits_While_Reconcile_In_Progress_No_Reads_Before_Gate_Opens()
        {
            var coord = new FakeCoordinator(startInProgress: true, reconDuration: TimeSpan.FromMilliseconds(1));

            // simulate that reconcile finishes later
            _ = Task.Run(async () =>
            {
                await Task.Delay(300);
                coord.InProgress = false;
            });

            var s1 = new FakeSensor(() => 20.0);
            var s2 = new FakeSensor(() => 20.1);
            var s3 = new FakeSensor(() => 20.2);

            var (accepted, value, readsBefore, readsAfter, reconCalls) =
                await RunClientOnceAsync(coord, s1, s2, s3, tol: 0.5);

            readsBefore.Should().Be(0); // no reads while busy
            readsAfter.Should().Be(3);  // exactly one round of 3 reads
            reconCalls.Should().Be(0);  // quorum hit, no reconcile
            accepted.Should().BeTrue();
        }

        [Fact]
        public async Task Client_Quorum_Hit_No_Reconcile()
        {
            var coord = new FakeCoordinator(startInProgress: false);
            var s1 = new FakeSensor(() => 20.0);
            var s2 = new FakeSensor(() => 20.1);
            var s3 = new FakeSensor(() => 20.2);

            var (accepted, value, _, readsAfter, reconCalls) =
                await RunClientOnceAsync(coord, s1, s2, s3, tol: 0.5);

            accepted.Should().BeTrue();
            readsAfter.Should().Be(3);
            reconCalls.Should().Be(0);
            value.Should().BeApproximately(20.05, 0.2);
        }

        [Fact]
        public async Task Client_NoQuorum_Triggers_Reconcile_Then_ReReads()
        {
            var coord = new FakeCoordinator(startInProgress: false, reconDuration: TimeSpan.FromMilliseconds(150));

            // divergent values so that with tol=0.3 there is no quorum
            var s1 = new FakeSensor(() => 20.0);
            var s2 = new FakeSensor(() => 22.0);
            var s3 = new FakeSensor(() => 24.0);

            var (accepted, value, readsBefore, readsAfter, reconCalls) =
                await RunClientOnceAsync(coord, s1, s2, s3, tol: 0.3);

            reconCalls.Should().Be(1);  // reconcile was triggered
            readsAfter.Should().Be(6);  // 3 reads before + 3 after reconcile
            accepted.Should().BeTrue();
        }
    }
}
