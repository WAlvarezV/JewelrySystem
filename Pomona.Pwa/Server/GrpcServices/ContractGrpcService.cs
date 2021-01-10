using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Pomona.Application.Interfaces;
using Pomona.Protos;
using System.Threading;
using System.Threading.Tasks;
using static Pomona.Protos.ContractSrv;

namespace Pomona.Pwa.Server.GrpcServices
{
    internal class ContractGrpcService : ContractSrvBase
    {
        private readonly IContractService _service;
        public ContractGrpcService(IContractService service) => _service = service;

        public override async Task<ContractResponse> RegisterContract(ContractProto contract, ServerCallContext context)
        => await _service.RegisterContractAsync(contract, CancellationToken.None);

        public override async Task<ContractsResponse> GetContracts(Empty empty, ServerCallContext context)
        => await _service.GetContractsAsync(empty, CancellationToken.None);

        public override async Task<ContractResponse> GetContractById(IdProto id, ServerCallContext context)
        => await _service.GetContractByIdAsync(id, CancellationToken.None);

        public override async Task<ContractResponse> RegisterPayment(PaymentProto payment, ServerCallContext context)
        => await _service.RegisterPaymentAsync(payment, CancellationToken.None);

    }
}
