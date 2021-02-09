using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using NLog;
using Pomona.Application.Interfaces;
using Pomona.Domain.Entity;
using Pomona.Domain.Enum;
using Pomona.Infrastructure.Implementation;
using Pomona.Protos.Common;
using Pomona.Protos.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pomona.Application.Services
{
    class ContractService : IContractService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public ContractService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<ContractResponse> RegisterContractAsync(ContractProto contract, CancellationToken cancelToken)
        {
            try
            {
                var client = new Person();
                var persons = await _uow.Persons.FindAll(x => x.IdentificationNumber.Equals(contract.IdentificationNumber));
                if (persons.Any())
                {
                    client = persons.First();
                }
                else
                {
                    client = _mapper.Map<Person>(contract);
                    client.PersonType = PersonType.Cliente;
                    _uow.Persons.Insert(client);
                    _uow.Save();
                }
                var contractToInsert = _mapper.Map<Contract>(contract);
                contractToInsert.PersonId = client.Id;
                contractToInsert.State = ContractState.Registrado;
                _uow.Contracts.Insert(contractToInsert);
                _uow.Save();

                var payment = new Payment
                {
                    EntityId = contractToInsert.Id,
                    Date = contractToInsert.Date,
                    PaymentType = PaymentType.Contrato,
                    Value = contract.Payment
                };
                _uow.Payments.Insert(payment);
                _uow.Save();
                var response = $"Contrato número {contract.Number} registrado.";
                return await Task.FromResult(new ContractResponse { Response = response }); ;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"RegisterContractAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}");
                return null;
            }
        }

        public async Task<ContractsResponse> GetContractsAsync(Empty empty, CancellationToken cancelToken)
        {
            try
            {
                var response = new ContractsResponse();
                var contracts = await _uow.Contracts.GetAll();
                var mapped = _mapper.Map<IEnumerable<ContractProto>>(contracts);
                response.ItemsList.AddRange(mapped);
                return response;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"GetContractsAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}");
                return null;
            }
        }

        public async Task<ContractResponse> GetContractByIdAsync(IdProto id, CancellationToken cancelToken)
        {
            try
            {
                var response = new ContractResponse();
                var contracts = await _uow.Contracts.FindAll(x => x.Id.Equals(id.Id), null, "Person");
                if (contracts.Any())
                {
                    var mapped = _mapper.Map<ContractProto>(contracts.First());
                    response.Contract = mapped;
                    var payments = await _uow.Payments.FindAll(x => x.EntityId.Equals(mapped.Id));
                    if (payments.Any())
                        response.Payments.AddRange(_mapper.Map<IEnumerable<PaymentProto>>(payments));
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"GetContractByIdAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}");
                return null;
            }
        }

        public async Task<ContractResponse> RegisterPaymentAsync(PaymentProto payment, CancellationToken cancelToken)
        {
            try
            {
                var contract = _uow.Contracts.GetById(payment.EntityId);

                var paymentToInsert = new Payment
                {
                    EntityId = payment.EntityId,
                    Date = DateTime.Now,
                    PaymentType = PaymentType.Contrato,
                    Value = payment.Value
                };
                _uow.Payments.Insert(paymentToInsert);
                _uow.Save();
                var payments = await _uow.Payments.FindAll(x => x.EntityId.Equals(payment.EntityId));
                contract.Balance = contract.Value - payments.Sum(x => x.Value);
                _uow.Save();
                var response = new ContractResponse { Response = "Abono registrado.", Contract = _mapper.Map<ContractProto>(contract) };
                response.Payments.AddRange(_mapper.Map<IEnumerable<PaymentProto>>(payments));
                return await Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"RegisterPaymentAsync ExceptionError => {ex.Message} {(ex.InnerException != null ? $"InnerExceptionError => {ex.InnerException.Message}" : "")}");
                return null;
            }
        }

    }
}
