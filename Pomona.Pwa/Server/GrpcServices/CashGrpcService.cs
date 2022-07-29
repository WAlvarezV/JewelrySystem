using Google.Protobuf.WellKnownTypes;
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

        public override async Task<DailyRecords> DailyRecord(Record record, ServerCallContext context)
            => await _service.DailyRecordAsync(record, CancellationToken.None);

        public override async Task<DailyRecords> GetDailyRecords(Empty empty, ServerCallContext context)
            => await _service.GetDailyRecordsAsync(empty, CancellationToken.None);

    }
}
