using Pomona.Domain.Entity;
using Pomona.Persistence.Repository;

namespace Pomona.Infrastructure.Implementation
{
    internal interface IUnitOfWork
    {
        Repository<DailyRecord> DailyRecords { get; }
        Repository<Contract> Contracts { get; }
        Repository<Payment> Payments { get; }
        Repository<Person> Persons { get; }
        Repository<ItemType> ItemTypes { get; }

        int Save();
    }
}
