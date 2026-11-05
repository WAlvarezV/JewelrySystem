namespace JewerlySystem.Common.Application.Documentation.DTOs;

/// <summary>
/// DTO con el contenido Markdown procesado y el HTML renderizado para la visualización de manuales de usuario.
/// </summary>
/// <param name="Title">Título descriptivo del manual o sección.</param>
/// <param name="MarkdownContent">Contenido original en formato Markdown.</param>
/// <param name="HtmlContent">Contenido procesado en formato HTML para renderizado seguro.</param>
public sealed record DocumentationPageDto(
    string Title,
    string MarkdownContent,
    string HtmlContent
);
