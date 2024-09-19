using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using NLog;
using Pomona.Application.Interfaces;
using Pomona.Domain.Entity;
using Pomona.Infrastructure.Implementation;
using Pomona.Protos.Common;
using Pomona.Protos.Inventory;
using Pomona.Utilities.Expressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public async Task<Response> RegisterItemAsync(ItemProto item, CancellationToken cancelToken)
        {
            var respose = new Response();

            var dbItem = (await _uow.Items
                .FindAll(x => x.Reference.Equals(item.Reference)
                && x.ItemTypeId.Equals(item.ItemTypeId)
                && x.Active, null, "ItemType")
                .ConfigureAwait(false))
                .FirstOrDefault(defaultValue: null);

            if (dbItem is not null)
            {
                respose.Message = $"ya se encuentra un {dbItem.ItemType.Name} registrado, con referencia: {dbItem.Reference}";
                respose.StatusCode = Code.Failed;
                return respose;
            }



            try
            {
                var newItem = _mapper.Map<Item>(item);
                if (item.ItemTypeId.Equals(5))
                {
                    var newWatch = _mapper.Map<Watch>(item);
                    newWatch.Item = newItem;
                    newWatch = _uow.Watches.Insert(newWatch);
                }
                else
                {
                    var newJewel = _mapper.Map<Jewel>(item);
                    newJewel.Item = newItem;
                    newJewel = _uow.Jewelry.Insert(newJewel);
                }
                _uow.Save();
                respose.StatusCode = Code.Ok;

            }
            catch (Exception ex)
            {
                var error = $"RegisterItemAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}";
                _logger.Error(ex, error);
                respose.Message = error;
                respose.StatusCode = Code.Failed;
            }
            return respose;
        }

        private async Task<bool> ItemExistsAsync(ItemProto item)
        => await _uow.Items
                .AnyAsync(x => x.Reference.Equals(item.Reference) && x.ItemTypeId.Equals(item.ItemTypeId))
                .ConfigureAwait(false);

        public async Task<WatchesResponse> GetWatchesAsync(Pagination pagination, CancellationToken cancelToken)
        {
            var watchesResponse = new WatchesResponse();
            var response = new Response();
            watchesResponse.Response = response;
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
                        Expression<Func<Watch, bool>> expressionOther = x => x.CaseNumber.Contains(filter.Other);
                        expression = ExpressionFunctions.AndAlso(expression, expressionOther);
                    }
                }

                var paginationResponse = await (isFilter ? _uow.Watches.FindAndPaginate(expression, pagination, "Item,Brand") : _uow.Watches.Paginate(pagination, "Item,Brand"));
                watchesResponse.Watches.AddRange(_mapper.Map<IEnumerable<WatchProto>>(paginationResponse.Items));
                watchesResponse.Pages = paginationResponse.Pages;
                watchesResponse.Response.Message = "Ok";
            }
            catch (Exception ex)
            {
                var error = $"GetWatchesAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}";
                _logger.Error(ex, error);
                watchesResponse.Response.Message = error;
            }
            return watchesResponse;
        }

        public async Task<JewelsResponse> GetJewelsAsync(Pagination pagination, CancellationToken cancelToken)
        {
            var jewelsResponse = new JewelsResponse();
            var response = new Response();
            jewelsResponse.Response = response;
            try
            {
                var isFilter = false;
                Expression<Func<Jewel, bool>> expression = x => x != null;
                if (pagination.Filter != null)
                {
                    var filter = pagination.Filter;
                    if (!string.IsNullOrWhiteSpace(filter.Key))
                    {
                        isFilter = true;
                        expression = x => x.Item.Reference.Equals(int.Parse(filter.Key));
                    }

                    if (!filter.Type.Equals("0"))
                    {
                        isFilter = true;
                        Expression<Func<Jewel, bool>> expressionType = x => x.Item.ItemTypeId.Equals(int.Parse(filter.Type));
                        expression = ExpressionFunctions.AndAlso<Jewel>(expression, expressionType);
                    }

                    if (!string.IsNullOrWhiteSpace(filter.Other))
                    {
                        isFilter = true;
                        Expression<Func<Jewel, bool>> expressionOther = x => x.Item.Description.Contains(filter.Other);
                        expression = ExpressionFunctions.AndAlso<Jewel>(expression, expressionOther);
                    }
                }

                var paginationResponse = await (isFilter ? _uow.Jewelry.FindAndPaginate(expression, pagination, "Item") : _uow.Jewelry.Paginate(pagination, "Item"));
                jewelsResponse.Jewels.AddRange(_mapper.Map<IEnumerable<JewelProto>>(paginationResponse.Items));
                jewelsResponse.Pages = paginationResponse.Pages;
                jewelsResponse.Response.Message = "Ok";
            }
            catch (Exception ex)
            {
                var error = $"GetJewelsAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}";
                _logger.Error(ex, error);
                jewelsResponse.Response.Message = error;
            }
            return jewelsResponse;
        }

        public async Task<ConsolidatedResponse> GetConsolidatedAsync(Empty empty, CancellationToken cancelToken)
        {
            var response = new ConsolidatedResponse();
            var itemTypes = await _uow.ItemTypes.GetAll();

            var jewerly = await _uow.Jewelry.FindAll(x => x.Item.Active.Equals(true), null, "Item");

            foreach (var jewel in jewerly)
            {
                jewel.Item.ItemType = itemTypes.Single(x => x.Id.Equals(jewel.Item.ItemTypeId));
            }


            var jewerlyConsolidated = jewerly.AsEnumerable().GroupBy(g => g.Item.ItemTypeId)
                         .Select(x =>
                               new Consolidated
                               {
                                   Type = x.First().Item.ItemType.Name,
                                   Quantity = x.Count(),
                                   Weight = x.Sum(j => j.Weight),
                                   CostValue = x.Sum(i => i.Item.CostValue),
                                   SaleValue = x.Sum(i => (int)i.Item.SaleValue)
                               }).OrderBy(o => o.Type);

            response.Jewerly.AddRange(jewerlyConsolidated);

            var watches = await _uow.Watches.FindAll(x => x.Item.Active.Equals(true), null, "Item,Brand");

            var watchesConsolidated = watches.AsEnumerable().GroupBy(g => new { g.BrandId, g.GenderType })
                         .Select(x =>
                               new Consolidated
                               {
                                   Type = x.First().Brand.Name,
                                   Other = x.First().GenderType.ToString(),
                                   Quantity = x.Count(),
                                   CostValue = x.Sum(i => i.Item.CostValue),
                                   SaleValue = x.Sum(i => (int)i.Item.SaleValue)
                               }).OrderBy(o => o.Type);

            response.Watches.AddRange(watchesConsolidated);


            return response;
        }

        public async Task<Response> DischargeItemAsync(DischargeRequest discharge, CancellationToken cancelToken)
        {
            var respose = new Response();
            try
            {
                var item = _uow.Items.GetById(discharge.ItemId);
                item.DateOfSale = DateTime.Now;
                item.SaleValue = discharge.SaleValue;
                item.Active = false;
                _uow.Items.Update(item);
                _uow.Save();
            }
            catch (Exception ex)
            {
                var error = $"DischargeItemAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}";
                _logger.Error(ex, error);
                respose.Message = error;
            }
            return await Task.FromResult(respose);
        }

        public async Task<ItemProto> GetItemAsync(ItemRequest request, CancellationToken cancelToken)
        {
            var respose = new ItemProto();
            try
            {
                Expression<Func<Item, bool>> expression = x => x.ItemTypeId.Equals(request.ItemTypeId);
                if (request.Reference > 0)
                {
                    Expression<Func<Item, bool>> expressionReference = x => x.Reference.Equals(request.Reference);
                    expression = ExpressionFunctions.AndAlso(expression, expressionReference);
                }

                if (!string.IsNullOrWhiteSpace(request.Description))
                {
                    Expression<Func<Item, bool>> expressionOther = x => x.Description.Contains(request.Description);
                    expression = ExpressionFunctions.AndAlso(expression, expressionOther);
                }

                var item = (await _uow.Items.FindAll(expression, null, "ItemType,Provider")).First();

                if (item.ItemTypeId.Equals(5))
                {
                    var watch = (await _uow.Watches.FindAll(x => x.ItemId.Equals(item.Id), null, "Brand")).First();
                    watch.Item = item;
                    respose = _mapper.Map<ItemProto>(watch);
                }
                else
                {
                    var jewel = (await _uow.Jewelry.FindAll(x => x.ItemId.Equals(item.Id))).First();
                    jewel.Item = item;
                    respose = _mapper.Map<ItemProto>(jewel);
                }

                var imagePath = item.GetImagePath();
                if (File.Exists(imagePath))
                {
                    respose.ImageName = Path.GetFileName(imagePath);
                    var bytes = File.ReadAllBytes(imagePath);
                    respose.Base64String = Convert.ToBase64String(bytes);
                }
            }
            catch (Exception ex)
            {
                var error = $"RegisterItemAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}";
                _logger.Error(ex, error);
            }
            return await Task.FromResult(respose);
        }
    }
}
