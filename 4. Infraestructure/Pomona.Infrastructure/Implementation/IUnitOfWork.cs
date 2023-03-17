using Pomona.Domain.Entity;
using Pomona.Models.Transient;
using Pomona.Persistence.Repository;

namespace Pomona.Infrastructure.Implementation
{
    internal interface IUnitOfWork
    {
        Repository<Brand> Brands { get; }
        Repository<Contract> Contracts { get; }
        Repository<DailyRecord> DailyRecords { get; }
        Repository<IdentificationType> IdentificationTypes { get; }
        Repository<Invoice> Invoices { get; }
        Repository<Item> Items { get; }
        Repository<ItemType> ItemTypes { get; }
        Repository<Jewel> Jewelry { get; }
        Repository<Payment> Payments { get; }
        Repository<Person> Persons { get; }
        Repository<Watch> Watches { get; }
        Repository<ConsolidatedRecord> ConsolidatedRecords { get; }

        int Save();
    }
}
