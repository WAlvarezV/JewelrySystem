using Pomona.Domain.Enum;

namespace Pomona.Domain.Entity
{
    internal class Person : BaseEntity
    {
        public int IdentificationTypeId { get; set; }
        public IdentificationType IdentificationType { get; set; }
        public string IdentificationNumber { get; set; }
        public string FullName { get; set; }
        public string CellPhone { get; set; }
        public string Email { get; set; }
        public PersonType PersonType { get; set; }
    }
}
