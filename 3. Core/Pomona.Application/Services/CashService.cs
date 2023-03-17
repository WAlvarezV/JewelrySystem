using AutoMapper;
using NLog;
using Pomona.Application.Interfaces;
using Pomona.Domain.Entity;
using Pomona.Infrastructure.Implementation;
using Pomona.Protos.Cash;
using Pomona.Protos.Common;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<DailyRecords> RegisterDailyRecordAsync(Record record, CancellationToken cancelToken)
        {
            var dailyResponse = new DailyRecords();
            try
            {
                var response = new Response();
                var today = Convert.ToDateTime(record.Date);
                TimeSpan ts = new(0, 0, 0);
                var startDate = today.Date + ts;
                ts = new TimeSpan(23, 59, 59);
                var endDate = today.Date + ts;

                var newDailyRecord = _mapper.Map<DailyRecord>(record);
                var inserted = _uow.DailyRecords.Insert(newDailyRecord);
                newDailyRecord.ItemId = record.ItemId > 0 ? record.ItemId : null;
                if (_uow.Save() > 0)
                {
                    response.Message = "Resgistro agregado correctamente";
                }
                else
                {
                    response.Message = "Resgistro no agregado";
                }
                var dailyRecords = await _uow.DailyRecords.FindAll(x => x.Date >= startDate && x.Date <= endDate);
                if (dailyRecords.Any())
                    dailyResponse.Items.AddRange(_mapper.Map<IEnumerable<Record>>(dailyRecords));
            }
            catch (Exception ex)
            {
                var error = $"RegisterDailyRecordAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}";
                Console.WriteLine(error);
            }


            return dailyResponse;
        }

        public async Task<DailyRecords> GetDailyRecordsAsync(RecordsRequest request, CancellationToken cancelToken)
        {
            var response = new DailyRecords();
            try
            {
                var toProcessRecords = _uow.ConsolidatedRecords.FromSqlRaw(SqlRaw.SqlRawQuerys.GetConsolidatedDailyRecordsQuery(request.StartDate, request.EndDate));
            }
            catch (Exception ex)
            {
                var error = $"RegisterDailyRecordAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}";
                Console.WriteLine(error);
            }

            var startDate = Convert.ToDateTime(request.StartDate);
            var endDate = Convert.ToDateTime(request.EndDate);

            var dailyRecords = await _uow.DailyRecords.FindAll(x => x.Date >= startDate && x.Date <= endDate);
            if (dailyRecords.Any())
                response.Items.AddRange(_mapper.Map<IEnumerable<Record>>(dailyRecords));

            return response;
        }

        public Task<ConsolidatedRecords> GetConsolidatedRecordsAsync(RecordsRequest request, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }
    }
}
