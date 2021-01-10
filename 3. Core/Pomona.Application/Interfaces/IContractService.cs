
using Google.Protobuf.WellKnownTypes;
using Pomona.Protos;
using System.Threading;
using System.Threading.Tasks;

namespace Pomona.Application.Interfaces
{
    internal interface IContractService
    {
        Task<ContractResponse> RegisterContractAsync(ContractProto contract, CancellationToken cancelToken);
        Task<ContractsResponse> GetContractsAsync(Empty empty, CancellationToken cancelToken);
        Task<ContractResponse> GetContractByIdAsync(IdProto id, CancellationToken cancelToken);
        Task<ContractResponse> RegisterPaymentAsync(PaymentProto payment, CancellationToken cancelToken);
    }
}
