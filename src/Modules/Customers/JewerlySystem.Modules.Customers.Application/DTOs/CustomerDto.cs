using JewerlySystem.Modules.Customers.Domain;

namespace JewerlySystem.Modules.Customers.Application.DTOs;

public record PersonDto(
    int Id,
    int IdentificationTypeId,
    string IdentificationNumber,
    string FullName,
    string CellPhone,
    string Email,
    PersonType PersonType);

public record IdentificationTypeDto(
    int Id,
    string Name);
