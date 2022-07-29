using Google.Protobuf.WellKnownTypes;
using Pomona.Protos.Cash;
using System.Threading;
using System.Threading.Tasks;

namespace Pomona.Application.Interfaces
{
    internal interface ICashService
    {
        Task<DailyRecords> DailyRecordAsync(Record record, CancellationToken cancelToken);
        Task<DailyRecords> GetDailyRecordsAsync(Empty empty, CancellationToken cancelToken);
    }
}
