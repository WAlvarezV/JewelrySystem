using System.ComponentModel.DataAnnotations;

namespace Pomona.Domain.Entity
{
    internal class BaseEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
