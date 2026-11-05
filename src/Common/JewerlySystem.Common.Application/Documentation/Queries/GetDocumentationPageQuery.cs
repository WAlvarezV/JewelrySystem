using System;
using System.Threading;
using System.Threading.Tasks;
using JewerlySystem.Common.Application.Documentation.Abstractions;
using JewerlySystem.Common.Application.Documentation.DTOs;
using JewerlySystem.Common.Domain;
using Microsoft.Extensions.Logging;

namespace JewerlySystem.Common.Application.Documentation.Queries;

/// <summary>
/// Query CQRS para solicitar la lectura y renderizado HTML de un archivo de documentación en Markdown.
/// </summary>
/// <param name="Title">Título para mostrar en la cabecera del drawer o página.</param>
/// <param name="FilePath">Ruta relativa dentro de wwwroot (ej. "docs/modules/Inventory/ManualUsuario.md").</param>
public sealed record GetDocumentationPageQuery(string Title, string FilePath) : IQuery<DocumentationPageDto>;

public sealed class GetDocumentationPageQueryHandler(
    IDocumentationProvider documentationProvider,
    ILogger<GetDocumentationPageQueryHandler> logger)
    : IQueryHandler<GetDocumentationPageQuery, DocumentationPageDto>
{
    public async Task<Result<DocumentationPageDto>> Handle(
        GetDocumentationPageQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            return Result.Failure<DocumentationPageDto>(Error.Validation("Documentation.InvalidPath", "La ruta del archivo de ayuda no puede ser nula ni vacía."));
        }

        try
        {
            (string markdown, string html) = await documentationProvider.GetContentAsync(request.FilePath, cancellationToken);

            if (string.IsNullOrEmpty(markdown))
            {
                logger.LogWarning("El archivo de documentación en la ruta '{FilePath}' no existe o está vacío.", request.FilePath);
                return Result.Failure<DocumentationPageDto>(Error.NotFound("Documentation.NotFound", $"No se encontró la documentación en la ruta especificada: {request.FilePath}"));
            }

            return Result.Success(new DocumentationPageDto(request.Title, markdown, html));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Excepción inesperada al procesar el manual de usuario en '{FilePath}'.", request.FilePath);
            return Result.Failure<DocumentationPageDto>(Error.Failure("Documentation.Error", "Ocurrió un error inesperado al procesar el manual de usuario."));
        }
    }
}
