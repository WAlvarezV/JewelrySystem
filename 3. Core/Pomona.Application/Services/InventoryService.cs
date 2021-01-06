using AutoMapper;
using NLog;
using Pomona.Application.Interfaces;
using Pomona.Infrastructure.Implementation;
using Pomona.Protos;
using System;
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
    }
}
