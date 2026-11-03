namespace JewerlySystem.Common.Domain;

/// <summary>
/// Bitácora persistente de eventos y acciones operativas de usuario
/// utilizada para auditoría física, control de gestión y evaluación de productividad ponderada.
/// </summary>
public class UserActivityLog : Entity<long>
{
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty; // e.g. "INVENTORY", "CONTRACTS", "INVOICING"
    public string ActionKey { get; set; } = string.Empty; // e.g. "Inventory_CreateJewel"
    public string Category { get; set; } = string.Empty;  // "Nuevo Registro", "Actualización", "Auditoría Rápida"
    public string Description { get; set; } = string.Empty;
    public int WeightScore { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserActivityLog() { }

    public UserActivityLog(
        string tenantId,
        string userId,
        string userName,
        string moduleKey,
        string actionKey,
        string category,
        string description,
        int weightScore,
        string entityName,
        string? entityReference = null)
    {
        TenantId = tenantId;
        UserId = userId;
        UserName = userName;
        ModuleKey = moduleKey;
        ActionKey = actionKey;
        Category = category;
        Description = description;
        WeightScore = weightScore;
        EntityName = entityName;
        EntityReference = entityReference;
        CreatedAt = DateTime.UtcNow;
    }
}
