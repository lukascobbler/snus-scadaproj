using System.ServiceModel;
using Shared.Contracts;

namespace Client.CLI.Contracts
{
    [ServiceContract(Name = "ISensorService", Namespace = "http://tempuri.org/")]
    public interface ISensorServiceClient
    {
        [OperationContract] double GetLatest();
        [OperationContract] SensorSnapshot GetSnapshot(TimeSpan lookback);
        [OperationContract] void Start();
        [OperationContract] void Stop();
        [OperationContract] void AppendReconciled(double value);
    }


    [ServiceContract(Name = "ICoordinatorService", Namespace = "http://tempuri.org/")]
    public interface ICoordinatorServiceClient
    {
        [OperationContract(
            Action = "http://tempuri.org/ICoordinatorService/ReconcileAsync",
            ReplyAction = "*")]
        System.Threading.Tasks.Task<ReconResult> ReconcileAsync();

        [OperationContract(
            Action = "http://tempuri.org/ICoordinatorService/IsReconInProgress",
            ReplyAction = "*")]
        bool IsReconInProgress();
    }
}
