using Pomona.Protos;
using System.Threading;
using System.Threading.Tasks;

namespace Pomona.Application.Interfaces
{
    internal interface IInventoryService
    {
        Task<ItemResponse> RegisterItemAsync(ItemProto item, CancellationToken cancelToken);
    }
}
