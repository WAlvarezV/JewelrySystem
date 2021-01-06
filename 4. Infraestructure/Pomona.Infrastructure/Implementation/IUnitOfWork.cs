using Pomona.Domain.Entity;
using Pomona.Persistence.Repository;

namespace Pomona.Infrastructure.Implementation
{
    internal interface IUnitOfWork
    {
        Repository<ItemType> ItemTypes { get; }
        Repository<Contract> Contracts { get; }

        int Save();
    }
}
