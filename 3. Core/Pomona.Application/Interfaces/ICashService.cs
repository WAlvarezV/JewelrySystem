using Pomona.Protos.Cash;
using System.Threading;
using System.Threading.Tasks;

namespace Pomona.Application.Interfaces
{
    internal interface ICashService
    {
        Task<DailyRecords> RegisterDailyRecordAsync(Record record, CancellationToken cancelToken);
        Task<DailyRecords> GetDailyRecordsAsync(RecordsRequest request, CancellationToken cancelToken);
        Task<ConsolidatedRecordsResponse> GetConsolidatedRecordsAsync(RecordsRequest request, CancellationToken cancelToken);
    }
}
