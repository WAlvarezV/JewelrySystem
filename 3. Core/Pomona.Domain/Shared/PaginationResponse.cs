using System.Collections.Generic;

namespace Pomona.Domain.Shared
{
    public class PaginationResponse<T>
    {
        public List<T> Items { get; set; }
        public int Pages { get; set; }
    }
}
