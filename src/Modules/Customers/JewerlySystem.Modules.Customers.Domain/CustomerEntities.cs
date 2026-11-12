using JewerlySystem.Common.Domain;

namespace JewerlySystem.Modules.Customers.Domain;

public enum PersonType
{
    Cliente = 1,
    Proveedor = 2,
    Employee = 3
}

public class IdentificationType : Entity<int>
{
    public string Name { get; set; } = string.Empty;

    public IdentificationType() { }
    public IdentificationType(int id, string name) : base(id)
    {
        Name = name;
    }
}

public class Person : AuditableEntity<int>
{
    public string? UserId { get; set; }
    public int IdentificationTypeId { get; set; }
    public string IdentificationNumber { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? FirstLastName { get; set; }
    public string? SecondLastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? CellPhone { get; set; }
    public string? Email { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime? HireDate { get; set; }
    public PersonType PersonType { get; set; }

    public Person() { }

    public Person(
        int identificationTypeId,
        string identificationNumber,
        string fullName,
        string cellPhone,
        string email,
        PersonType personType,
        string? userId = null,
        string? firstName = null,
        string? firstLastName = null,
        string? secondLastName = null,
        string? address = null,
        DateTime? birthDate = null,
        DateTime? hireDate = null)
    {
        IdentificationTypeId = identificationTypeId;
        IdentificationNumber = identificationNumber ?? string.Empty;
        FullName = fullName ?? string.Empty;
        CellPhone = cellPhone ?? string.Empty;
        Email = email ?? string.Empty;
        PersonType = personType;
        UserId = userId;
        FirstName = firstName ?? string.Empty;
        FirstLastName = firstLastName ?? string.Empty;
        SecondLastName = secondLastName;
        Address = address;
        BirthDate = birthDate;
        HireDate = hireDate;
    }
}
