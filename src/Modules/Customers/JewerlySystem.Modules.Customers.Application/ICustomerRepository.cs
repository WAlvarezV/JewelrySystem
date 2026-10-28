using JewerlySystem.Modules.Customers.Domain;

namespace JewerlySystem.Modules.Customers.Application;

public interface ICustomerRepository
{
    Task<Person?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Person?> GetByIdentificationAsync(string number, CancellationToken ct = default);
    Task<IReadOnlyList<Person>> ListAsync(PersonType? type = null, string? searchTerm = null, CancellationToken ct = default);
    Task<IReadOnlyList<IdentificationType>> GetIdentificationTypesAsync(CancellationToken ct = default);
    Task AddAsync(Person person, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
