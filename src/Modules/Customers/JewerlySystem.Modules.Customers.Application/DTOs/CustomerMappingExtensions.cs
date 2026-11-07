using System.Linq;
using System.Linq.Expressions;
using JewerlySystem.Modules.Customers.Application.DTOs;
using JewerlySystem.Modules.Customers.Domain;

namespace JewerlySystem.Modules.Customers.Application.DTOs;

public static class CustomerMappingExtensions
{
    public static readonly Expression<Func<Person, PersonDto>> PersonDtoProjection = p => new PersonDto(
        p.Id,
        p.IdentificationTypeId,
        p.IdentificationNumber,
        p.FullName,
        p.CellPhone,
        p.Email,
        p.PersonType,
        p.UserId,
        p.FirstName,
        p.FirstLastName,
        p.SecondLastName,
        p.Address,
        p.BirthDate,
        p.HireDate
    );

    public static readonly Expression<Func<IdentificationType, IdentificationTypeDto>> IdentificationTypeDtoProjection = i => new IdentificationTypeDto(
        i.Id,
        i.Name
    );

    public static PersonDto ToDto(this Person person) => new(
        person.Id,
        person.IdentificationTypeId,
        person.IdentificationNumber,
        person.FullName,
        person.CellPhone,
        person.Email,
        person.PersonType,
        person.UserId,
        person.FirstName,
        person.FirstLastName,
        person.SecondLastName,
        person.Address,
        person.BirthDate,
        person.HireDate
    );

    public static IdentificationTypeDto ToDto(this IdentificationType identificationType) => new(
        identificationType.Id,
        identificationType.Name
    );

    public static IQueryable<PersonDto> ProjectToDto(this IQueryable<Person> query)
    {
        return query.Select(PersonDtoProjection);
    }

    public static IQueryable<IdentificationTypeDto> ProjectToDto(this IQueryable<IdentificationType> query)
    {
        return query.Select(IdentificationTypeDtoProjection);
    }
}
