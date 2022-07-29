using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using NLog;
using Pomona.Application.Interfaces;
using Pomona.Infrastructure.Implementation;
using Pomona.Protos.Cash;
using System.Threading;
using System.Threading.Tasks;

namespace Pomona.Application.Services
{
    internal class CashService : ICashService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public CashService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public Task<DailyRecords> DailyRecordAsync(Record record, CancellationToken cancelToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<DailyRecords> GetDailyRecordsAsync(Empty empty, CancellationToken cancelToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
