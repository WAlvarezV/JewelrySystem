using JewerlySystem.Modules.Contracts.Domain;

namespace JewerlySystem.Modules.Contracts.Application.DTOs;

public record ContractDto(
    int Id,
    int Number,
    int? Reference,
    DateTime Date,
    DateTime DeliveryDate,
    int Value,
    ContractState State,
    int Balance,
    double Weight,
    string Description,
    int PersonId,
    int? ProviderId,
    int? FatherId);

public record PaymentDto(
    int Id,
    string Number,
    DateTime Date,
    int Value,
    int EntityId,
    PaymentType PaymentType,
    PaymentMethod PaymentMethod);
