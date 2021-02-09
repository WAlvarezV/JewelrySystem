using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Pomona.Application.Interfaces;
using Pomona.Protos.Parametric;
using System.Threading;
using System.Threading.Tasks;
using static Pomona.Protos.Parametric.ParametricSrv;

namespace Pomona.Pwa.Server.GrpcServices
{
    internal class ParametricGrpcService : ParametricSrvBase
    {
        private readonly IParametricService _service;
        public ParametricGrpcService(IParametricService service) => _service = service;

        public override async Task<TypesResponse> GetBrands(Empty empty, ServerCallContext context)
            => await _service.GetBrandsAsync(empty, CancellationToken.None);

        public override async Task<TypesResponse> GetDocumentTypes(Empty empty, ServerCallContext context)
            => await _service.GetDocumentTypesAsync(empty, CancellationToken.None);

        public override async Task<TypesResponse> GetGenders(Empty empty, ServerCallContext context)
            => await _service.GetGendersAsync(empty, CancellationToken.None);

        public override async Task<TypesResponse> GetProviders(Empty empty, ServerCallContext context)
            => await _service.GetProvidersAsync(empty, CancellationToken.None);

    }
}
