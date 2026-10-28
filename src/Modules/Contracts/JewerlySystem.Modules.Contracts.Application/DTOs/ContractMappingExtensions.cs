using System.Linq;
using System.Linq.Expressions;
using JewerlySystem.Modules.Contracts.Application.DTOs;
using JewerlySystem.Modules.Contracts.Domain;

namespace JewerlySystem.Modules.Contracts.Application.DTOs;

public static class ContractMappingExtensions
{
    public static readonly Expression<Func<Contract, ContractDto>> ContractDtoProjection = c => new ContractDto(
        c.Id,
        c.Number,
        c.Reference,
        c.Date,
        c.DeliveryDate,
        c.Value,
        c.State,
        c.Balance,
        c.Weight,
        c.Description,
        c.PersonId,
        c.ProviderId,
        c.FatherId
    );

    public static readonly Expression<Func<Payment, PaymentDto>> PaymentDtoProjection = p => new PaymentDto(
        p.Id,
        p.Number,
        p.Date,
        p.Value,
        p.EntityId,
        p.PaymentType,
        p.PaymentMethod
    );

    public static ContractDto ToDto(this Contract entity) => new(
        entity.Id,
        entity.Number,
        entity.Reference,
        entity.Date,
        entity.DeliveryDate,
        entity.Value,
        entity.State,
        entity.Balance,
        entity.Weight,
        entity.Description,
        entity.PersonId,
        entity.ProviderId,
        entity.FatherId
    );

    public static PaymentDto ToDto(this Payment entity) => new(
        entity.Id,
        entity.Number,
        entity.Date,
        entity.Value,
        entity.EntityId,
        entity.PaymentType,
        entity.PaymentMethod
    );

    public static IQueryable<ContractDto> ProjectToDto(this IQueryable<Contract> query)
    {
        return query.Select(ContractDtoProjection);
    }

    public static IQueryable<PaymentDto> ProjectToDto(this IQueryable<Payment> query)
    {
        return query.Select(PaymentDtoProjection);
    }
}
