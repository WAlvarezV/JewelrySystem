namespace JewerlySystem.Common.Presentation.Services;

/// <summary>
/// Información de ayuda contextual mapeada a una ruta de la aplicación.
/// </summary>
/// <param name="ModuleName">Nombre descriptivo del módulo (ej. "Inventario", "Contratos").</param>
/// <param name="DocumentPath">Ruta relativa del archivo Markdown en wwwroot (ej. "docs/modules/Inventory/ManualUsuario.md").</param>
/// <param name="SectionAnchor">Identificador opcional del título/sección a la que debe saltar (ej. "proceso-1-catalogo").</param>
/// <param name="CustomTitle">Título personalizado para el drawer o vista de ayuda.</param>
/// <param name="IsPublic">Indica si la pantalla es de acceso público sin sesión requerida.</param>
public sealed record HelpRouteInfo(
    string ModuleName,
    string? DocumentPath,
    string? SectionAnchor = null,
    string? CustomTitle = null,
    bool IsPublic = false
);
