using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Inventory.Application.DTOs;
using JewerlySystem.Modules.Inventory.Domain;

namespace JewerlySystem.Modules.Inventory.Application.Queries;

public record GetItemHistoryQuery(int Reference) : IQuery<ItemHistoryResponseDto>;

public class GetItemHistoryQueryHandler : IQueryHandler<GetItemHistoryQuery, ItemHistoryResponseDto>
{
    private readonly IInventoryRepository _repository;

    public GetItemHistoryQueryHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ItemHistoryResponseDto>> Handle(GetItemHistoryQuery request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetItemByReferenceAsync(request.Reference, cancellationToken);
        if (item == null)
        {
            return Result.Failure<ItemHistoryResponseDto>(Error.NotFound("Items.NotFound", $"Artículo con referencia #{request.Reference} no encontrado."));
        }

        var jewel = await _repository.GetJewelByItemIdAsync(item.Id, cancellationToken);
        var watch = await _repository.GetWatchByItemIdAsync(item.Id, cancellationToken);
        var itemTypes = await _repository.GetItemTypesAsync(cancellationToken);
        var brands = await _repository.GetBrandsAsync(cancellationToken);

        var itemTypeName = itemTypes.FirstOrDefault(t => t.Id == item.ItemTypeId)?.Name 
            ?? (jewel != null ? "Joya" : watch != null ? "Reloj" : "Artículo");
        var brandName = watch != null ? brands.FirstOrDefault(b => b.Id == watch.BrandId)?.Name : null;

        var rawLogs = await _repository.GetItemActivityLogsAsync(request.Reference.ToString(), cancellationToken);

        var events = new List<ItemHistoryEventDto>();

        // 1. Agregar registros existentes de UserActivityLog
        foreach (var log in rawLogs)
        {
            var (opName, tagColor, icon) = MapActionDetails(log.ActionKey, log.Category);

            events.Add(new ItemHistoryEventDto(
                Id: log.Id,
                Timestamp: log.CreatedAt,
                Operation: opName,
                PerformedBy: string.IsNullOrWhiteSpace(log.UserName) ? (log.UserId ?? "Sistema") : log.UserName,
                Description: log.Description,
                Category: log.Category,
                TagColor: tagColor,
                Icon: icon
            ));
        }

        // 2. Si no hay evento de creación en logs (ej. registros migrados o previos a la bitácora), sintetizar el evento de creación inicial
        bool hasCreationEvent = events.Any(e => 
            e.Operation.Contains("Creación", StringComparison.OrdinalIgnoreCase) || 
            e.Category == ActionWeightRegistry.CategoryNewRecord);

        if (!hasCreationEvent)
        {
            var creationDate = item.CreatedAt != default ? item.CreatedAt : (item.DateOfEntry != default ? item.DateOfEntry : DateTime.UtcNow);
            var createdBy = !string.IsNullOrWhiteSpace(item.CreatedBy) ? item.CreatedBy : "Sistema";

            string creationDesc;
            if (jewel != null)
            {
                creationDesc = $"Ingreso y creación de joya ({itemTypeName}) - {item.Description}. Peso: {jewel.Weight:F2}g, Valor gramo: ${jewel.GramValue:N0}, Costo inicial: ${item.CostValue:N0}";
            }
            else if (watch != null)
            {
                creationDesc = $"Ingreso y creación de reloj ({brandName ?? "Sin marca"}) - {item.Description}. Caja #{watch.CaseNumber}, Costo inicial: ${item.CostValue:N0}";
            }
            else
            {
                creationDesc = $"Ingreso y registro inicial del artículo - {item.Description}. Costo: ${item.CostValue:N0}";
            }

            events.Add(new ItemHistoryEventDto(
                Id: 0,
                Timestamp: creationDate,
                Operation: jewel != null ? "Creación de Joya" : (watch != null ? "Creación de Reloj" : "Creación de Artículo"),
                PerformedBy: createdBy,
                Description: creationDesc,
                Category: "Registro Inicial",
                TagColor: "blue",
                Icon: "plus-circle"
            ));
        }

        // 3. Si tiene fecha de última modificación y no está en los eventos recientes
        if (item.LastModifiedAt.HasValue)
        {
            bool hasEditNear = events.Any(e => Math.Abs((e.Timestamp - item.LastModifiedAt.Value).TotalSeconds) < 60);
            if (!hasEditNear)
            {
                events.Add(new ItemHistoryEventDto(
                    Id: 0,
                    Timestamp: item.LastModifiedAt.Value,
                    Operation: "Actualización de Artículo",
                    PerformedBy: !string.IsNullOrWhiteSpace(item.LastModifiedBy) ? item.LastModifiedBy : "Sistema",
                    Description: $"Modificación de datos del artículo - {item.Description}",
                    Category: "Actualización",
                    TagColor: "purple",
                    Icon: "edit"
                ));
            }
        }

        // 4. Si el artículo fue descargado por venta y no está en logs
        if (item.DateOfSale.HasValue)
        {
            bool hasSaleEvent = events.Any(e => 
                e.Operation.Contains("Venta", StringComparison.OrdinalIgnoreCase) || 
                e.Category == ActionWeightRegistry.CategoryDischarge);

            if (!hasSaleEvent)
            {
                events.Add(new ItemHistoryEventDto(
                    Id: 0,
                    Timestamp: item.DateOfSale.Value,
                    Operation: "Descargo por Venta",
                    PerformedBy: !string.IsNullOrWhiteSpace(item.LastModifiedBy) ? item.LastModifiedBy : "Sistema",
                    Description: $"Artículo descargado del catálogo por venta. Valor de venta: ${(item.SaleValue ?? 0):N0}",
                    Category: "Descargo",
                    TagColor: "red",
                    Icon: "export"
                ));
            }
        }

        // Ordenar cronológicamente descendente (el más reciente arriba)
        var orderedEvents = events.OrderByDescending(e => e.Timestamp).ToList();

        var response = new ItemHistoryResponseDto(
            Reference: item.Reference,
            Description: item.Description,
            ItemType: itemTypeName,
            Active: item.Active,
            CostValue: item.CostValue,
            Weight: jewel?.Weight,
            GramValue: jewel?.GramValue,
            CaseNumber: watch?.CaseNumber,
            BrandName: brandName,
            CreatedAt: item.CreatedAt != default ? item.CreatedAt : item.DateOfEntry,
            CreatedBy: item.CreatedBy ?? "Sistema",
            LastModifiedAt: item.LastModifiedAt,
            LastModifiedBy: item.LastModifiedBy,
            DateOfSale: item.DateOfSale,
            SaleValue: item.SaleValue,
            Events: orderedEvents
        );

        return Result.Success(response);
    }

    private static (string Operation, string TagColor, string Icon) MapActionDetails(string actionKey, string category)
    {
        return actionKey switch
        {
            ActionWeightRegistry.InventoryCreateJewel => ("Creación de Joya", "blue", "plus-circle"),
            ActionWeightRegistry.InventoryCreateWatch => ("Creación de Reloj", "blue", "plus-circle"),
            ActionWeightRegistry.InventoryUpdateJewel => ("Actualización de Joya", "purple", "edit"),
            ActionWeightRegistry.InventoryUpdateWatch => ("Actualización de Reloj", "purple", "edit"),
            ActionWeightRegistry.InventoryDischarge => ("Descargo por Venta", "red", "export"),
            ActionWeightRegistry.InventoryToggleActive => ("Auditoría de Catálogo", "cyan", "check-circle"),
            _ => (category switch
            {
                ActionWeightRegistry.CategoryNewRecord => "Creación",
                ActionWeightRegistry.CategoryUpdate => "Edición",
                ActionWeightRegistry.CategoryDischarge => "Descargo",
                ActionWeightRegistry.CategoryAuditQuick => "Auditoría",
                _ => "Operación de Inventario"
            }, "orange", "clock-circle")
        };
    }
}
