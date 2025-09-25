using System.ServiceModel; 
using System.Threading.Tasks;

namespace Shared.Contracts
{
    [ServiceContract(Name = "ICoordinatorService", Namespace = "http://tempuri.org/")]
    public interface ICoordinatorService
    {
        [OperationContract(
            Action = "http://tempuri.org/ICoordinatorService/ReconcileAsync",
            ReplyAction = "*")]
        Task<ReconResult> ReconcileAsync();

        [OperationContract(
            Action = "http://tempuri.org/ICoordinatorService/IsReconInProgress",
            ReplyAction = "*")]
        bool IsReconInProgress();
    }
}
