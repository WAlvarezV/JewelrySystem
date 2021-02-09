using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using NLog;
using Pomona.Application.Interfaces;
using Pomona.Domain.Enum;
using Pomona.Infrastructure.Implementation;
using Pomona.Protos.Common;
using Pomona.Protos.Parametric;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pomona.Application.Services
{
    class ParametricService : IParametricService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public ParametricService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<TypesResponse> GetBrandsAsync(Empty empty, CancellationToken cancelToken)
        {
            var response = new TypesResponse();
            try
            {
                var list = await _uow.Brands.GetAll();
                var mapped = _mapper.Map<IEnumerable<TypeProto>>(list);
                response.Items.AddRange(mapped);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"GetBrandsAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}");
            }
            return response;
        }

        public async Task<TypesResponse> GetDocumentTypesAsync(Empty empty, CancellationToken cancelToken)
        {
            var response = new TypesResponse();
            try
            {
                var list = await _uow.IdentificationTypes.GetAll();
                var mapped = _mapper.Map<IEnumerable<TypeProto>>(list);
                response.Items.AddRange(mapped);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"GetDocumentTypesAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}");
            }
            return response;
        }

        public async Task<TypesResponse> GetGendersAsync(Empty empty, CancellationToken cancelToken)
        {
            var response = new TypesResponse();
            try
            {
                response.Items.AddRange(new TypeProto[] { new TypeProto { Id = "1", Name = "MUJER" }, new TypeProto { Id = "2", Name = "HOMBRE" } });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"GetGendersAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}");
            }
            return await Task.FromResult(response);
        }

        public async Task<TypesResponse> GetProvidersAsync(Empty empty, CancellationToken cancelToken)
        {
            var response = new TypesResponse();
            try
            {
                var list = await _uow.Persons.FindAll(x => x.PersonType.Equals(PersonType.Proveedor));
                var mapped = _mapper.Map<IEnumerable<TypeProto>>(list);
                response.Items.AddRange(mapped);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"GetGendersAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}");
            }
            return await Task.FromResult(response);
        }
    }
}
