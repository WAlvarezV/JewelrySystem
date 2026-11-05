namespace JewerlySystem.Common.Presentation.Services;

/// <summary>
/// Contrato para la resolución contextual de ayuda y manuales según la ruta activa.
/// </summary>
public interface IHelpRouteRegistry
{
    HelpRouteInfo GetHelpInfoForRoute(string relativePath);
}
