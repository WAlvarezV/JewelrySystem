using JewerlySystem.Common.Domain;

namespace JewerlySystem.Modules.Customers.Domain;

public enum PersonType
{
    Cliente = 1,
    Proveedor = 2
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
    public int IdentificationTypeId { get; set; }
    public string IdentificationNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CellPhone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public PersonType PersonType { get; set; }

    public Person() { }

    public Person(
        int identificationTypeId,
        string identificationNumber,
        string fullName,
        string cellPhone,
        string email,
        PersonType personType)
    {
        IdentificationTypeId = identificationTypeId;
        IdentificationNumber = identificationNumber ?? string.Empty;
        FullName = fullName ?? string.Empty;
        CellPhone = cellPhone ?? string.Empty;
        Email = email ?? string.Empty;
        PersonType = personType;
    }
}
