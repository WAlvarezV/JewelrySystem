using JewerlySystem.Modules.Customers.Application;
using JewerlySystem.Modules.Customers.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Customers.Infrastructure;

public class CustomerRepository : ICustomerRepository
{
    private readonly CustomersDbContext _dbContext;

    public CustomerRepository(CustomersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Person?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Persons.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Person?> GetByIdentificationAsync(string number, CancellationToken ct = default)
    {
        return await _dbContext.Persons.FirstOrDefaultAsync(p => p.IdentificationNumber == number, ct);
    }

    public async Task<Person?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _dbContext.Persons.FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    public async Task<IReadOnlyList<Person>> ListAsync(PersonType? type = null, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = _dbContext.Persons.AsNoTracking().AsQueryable();

        if (type.HasValue)
        {
            query = query.Where(p => p.PersonType == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.FullName.Contains(searchTerm) || p.IdentificationNumber.Contains(searchTerm));
        }

        return await query.OrderBy(p => p.FullName).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<IdentificationType>> GetIdentificationTypesAsync(CancellationToken ct = default)
    {
        return await _dbContext.IdentificationTypes.AsNoTracking().OrderBy(i => i.Name).ToListAsync(ct);
    }

    public async Task AddAsync(Person person, CancellationToken ct = default)
    {
        await _dbContext.Persons.AddAsync(person, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
