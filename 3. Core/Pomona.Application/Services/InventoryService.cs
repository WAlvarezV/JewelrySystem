using AutoMapper;
using NLog;
using Pomona.Application.Interfaces;
using Pomona.Domain.Entity;
using Pomona.Infrastructure.Implementation;
using Pomona.Protos.Common;
using Pomona.Protos.Inventory;
using Pomona.Utilities.Expressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Pomona.Application.Services
{
    class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public InventoryService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public Task<ItemResponse> RegisterItemAsync(ItemProto item, CancellationToken cancelToken)
        {
            try
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"RegisterItemAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}");
                return null;
            }
        }

        public async Task<WatchesResponse> GetWatchesAsync(Pagination pagination, CancellationToken cancelToken)
        {
            var response = new WatchesResponse();
            try
            {

                var isFilter = false;
                Expression<Func<Watch, bool>> expression = x => x != null;
                if (pagination.Filter != null)
                {
                    var filter = pagination.Filter;
                    if (!string.IsNullOrWhiteSpace(filter.Key))
                    {
                        isFilter = true;
                        expression = x => x.Item.Reference.Equals(int.Parse(filter.Key));
                    }

                    if (!filter.State.Equals("0"))
                    {
                        //isFilter = true;
                        //Expression<Func<ViewAcreencia, bool>> expressionState = x => x.EstadoId.Equals(int.Parse(filter.State));
                        //expression = ExpressionFunctions.AndAlso<ViewAcreencia>(expression, expressionState);
                    }

                    if (!string.IsNullOrWhiteSpace(filter.Other))
                    {
                        isFilter = true;
                        Expression<Func<Watch, bool>> expressionOther = x => x.CaseNumber.Equals(filter.Other);
                        //expression = Expression.Lambda<Func<ViewAcreencia, bool>>(Expression.And(expression.Parameters, expressionState.Body));
                        expression = ExpressionFunctions.AndAlso<Watch>(expression, expressionOther);
                    }
                }

                var paginationResponse = await (isFilter ? _uow.Watches.FindAndPaginate(expression, pagination, "Item,Brand") : _uow.Watches.Paginate(pagination, "Item,Brand"));
                // var watches = await _uow.Watches.FindAll(null, null, "Item,Brand");
                response.Watches.AddRange(_mapper.Map<IEnumerable<WatchProto>>(paginationResponse.Items));
                response.Pages = paginationResponse.Pages;
                return response;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"GetWatchesAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}");
                return response;
            }
        }
    }
}
