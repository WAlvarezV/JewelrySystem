using JewerlySystem.Modules.Customers.Domain;

namespace JewerlySystem.Modules.Customers.Application.DTOs;

public record PersonDto(
    int Id,
    int IdentificationTypeId,
    string IdentificationNumber,
    string FullName,
    string CellPhone,
    string Email,
    PersonType PersonType,
    string? UserId = null,
    string? FirstName = null,
    string? FirstLastName = null,
    string? SecondLastName = null,
    string? Address = null,
    DateTime? BirthDate = null,
    DateTime? HireDate = null);

public record IdentificationTypeDto(
    int Id,
    string Name);
