using System.Threading;
using System.Threading.Tasks;

namespace JewerlySystem.Common.Application.Documentation.Abstractions;

public interface IDocumentationProvider
{
    Task<(string Markdown, string Html)> GetContentAsync(string relativePath, CancellationToken cancellationToken = default);
}
