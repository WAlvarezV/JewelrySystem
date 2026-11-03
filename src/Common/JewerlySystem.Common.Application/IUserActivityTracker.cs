namespace JewerlySystem.Common.Application;

/// <summary>
/// Contrato para el registro desacoplado y atómico de actividades y eventos operativos de usuarios.
/// </summary>
public interface IUserActivityTracker
{
    Task TrackActivityAsync(
        string actionKey,
        string category,
        string description,
        int? weightScore = null,
        string entityName = "Item",
        string? entityReference = null,
        CancellationToken cancellationToken = default);
}
