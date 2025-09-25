using System;
using System.ServiceModel;
using Shared.Contracts;

namespace Coordinator.Host.Clients
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
}
