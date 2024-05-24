using Grpc.Core;
using Pomona.Application.Interfaces;
using Pomona.Protos.Cash;
using System.Threading;
using System.Threading.Tasks;
using static Pomona.Protos.Cash.CashSrv;

namespace Pomona.Pwa.Server.GrpcServices
{
    internal class CashGrpcService : CashSrvBase
    {
        private readonly ICashService _service;
        public CashGrpcService(ICashService service) => _service = service;

        public override async Task<DailyRecords> RegisterDailyRecord(Record record, ServerCallContext context)
            => await _service.RegisterDailyRecordAsync(record, CancellationToken.None);

        public override async Task<DailyRecords> GetDailyRecords(RecordsRequest request, ServerCallContext context)
            => await _service.GetDailyRecordsAsync(request, CancellationToken.None);

        public override async Task<ConsolidatedRecordsResponse> GetConsolidatedRecords(RecordsRequest request, ServerCallContext context)
            => await _service.GetConsolidatedRecordsAsync(request, CancellationToken.None);

    }
}
