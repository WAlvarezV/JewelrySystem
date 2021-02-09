using Google.Protobuf.WellKnownTypes;
using Pomona.Protos.Parametric;
using System.Threading;
using System.Threading.Tasks;

namespace Pomona.Application.Interfaces
{
    internal interface IParametricService
    {
        Task<TypesResponse> GetBrandsAsync(Empty empty, CancellationToken cancelToken);
        Task<TypesResponse> GetDocumentTypesAsync(Empty empty, CancellationToken cancelToken);
        Task<TypesResponse> GetGendersAsync(Empty empty, CancellationToken cancelToken);
        Task<TypesResponse> GetProvidersAsync(Empty empty, CancellationToken cancelToken);
    }
}
