using FluentValidation;
using JewerlySystem.Common.Application;
using JewerlySystem.Common.Application.Inventory;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Inventory.Application.DTOs;
using JewerlySystem.Modules.Inventory.Domain;

namespace JewerlySystem.Modules.Inventory.Application.Commands;

public record CreateJewelItemCommand(
    int CostValue,
    DateTime DateOfEntry,
    string Description,
    int ItemTypeId,
    double Weight,
    int GramValue,
    int? Length = null,
    int? ProviderId = null,
    string? ImageUrl = null) : ICommand<CreateItemResponse>;

public class CreateJewelItemCommandValidator : AbstractValidator<CreateJewelItemCommand>
{
    public CreateJewelItemCommandValidator()
    {
        RuleFor(x => x.CostValue).GreaterThan(0).WithMessage("El valor de costo debe ser mayor a 0.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("La descripción es obligatoria.");
        RuleFor(x => x.Weight).GreaterThan(0).WithMessage("El peso debe ser mayor a 0.");
        RuleFor(x => x.GramValue).GreaterThan(0).WithMessage("El valor por gramo debe ser mayor a 0.");
    }
}

public class CreateJewelItemCommandHandler : ICommandHandler<CreateJewelItemCommand, CreateItemResponse>
{
    private readonly IInventoryRepository _repository;
    private readonly IUserActivityTracker? _activityTracker;

    public CreateJewelItemCommandHandler(IInventoryRepository repository, IUserActivityTracker? activityTracker = null)
    {
        _repository = repository;
        _activityTracker = activityTracker;
    }

    public async Task<Result<CreateItemResponse>> Handle(CreateJewelItemCommand request, CancellationToken cancellationToken)
    {
        var item = new Item(
            reference: 0,
            costValue: request.CostValue,
            dateOfEntry: request.DateOfEntry,
            description: request.Description,
            itemTypeId: request.ItemTypeId,
            providerId: request.ProviderId,
            imageUrl: request.ImageUrl,
            thumbnailUrl: request.ImageUrl
        );

        var jewel = new Jewel(
            itemId: 0,
            weight: request.Weight,
            gramValue: request.GramValue,
            length: request.Length
        );

        var response = await _repository.CreateJewelItemAtomicAsync(item, jewel, cancellationToken);
        if (response == null)
        {
            return Result.Failure<CreateItemResponse>(Error.Failure("Items.CreateFailed", "No se pudo registrar la joya en inventario."));
        }

        if (_activityTracker != null)
        {
            await _activityTracker.TrackActivityAsync(
                actionKey: ActionWeightRegistry.InventoryCreateJewel,
                category: ActionWeightRegistry.CategoryNewRecord,
                description: $"Creó Joya Ref #{response.Reference} - {request.Description} ({request.Weight:F2}g)",
                weightScore: 10,
                entityName: "Item",
                entityReference: response.Reference.ToString(),
                cancellationToken: cancellationToken);
        }

        return Result.Success(response);
    }
}

public record CreateWatchItemCommand(
    int CostValue,
    DateTime DateOfEntry,
    string Description,
    int ItemTypeId,
    GenderType GenderType,
    string CaseNumber,
    string Model,
    int BrandId,
    int? ProviderId = null,
    string? ImageUrl = null) : ICommand<CreateItemResponse>;

public class CreateWatchItemCommandValidator : AbstractValidator<CreateWatchItemCommand>
{
    public CreateWatchItemCommandValidator()
    {
        RuleFor(x => x.CostValue).GreaterThan(0).WithMessage("El valor de costo debe ser mayor a 0.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("La descripción es obligatoria.");
        RuleFor(x => x.CaseNumber).NotEmpty().WithMessage("El número de caja es obligatorio.");
        RuleFor(x => x.BrandId).GreaterThan(0).WithMessage("La marca es obligatoria.");
    }
}

public class CreateWatchItemCommandHandler : ICommandHandler<CreateWatchItemCommand, CreateItemResponse>
{
    private readonly IInventoryRepository _repository;
    private readonly IUserActivityTracker? _activityTracker;

    public CreateWatchItemCommandHandler(IInventoryRepository repository, IUserActivityTracker? activityTracker = null)
    {
        _repository = repository;
        _activityTracker = activityTracker;
    }

    public async Task<Result<CreateItemResponse>> Handle(CreateWatchItemCommand request, CancellationToken cancellationToken)
    {
        var item = new Item(
            reference: 0,
            costValue: request.CostValue,
            dateOfEntry: request.DateOfEntry,
            description: request.Description,
            itemTypeId: request.ItemTypeId,
            providerId: request.ProviderId,
            imageUrl: request.ImageUrl,
            thumbnailUrl: request.ImageUrl
        );

        var watch = new Watch(
            itemId: 0,
            genderType: request.GenderType,
            caseNumber: request.CaseNumber,
            brandId: request.BrandId,
            model: request.Model
        );

        var response = await _repository.CreateWatchItemAtomicAsync(item, watch, cancellationToken);
        if (response == null)
        {
            return Result.Failure<CreateItemResponse>(Error.Failure("Items.CreateFailed", "No se pudo registrar el reloj en inventario."));
        }

        if (_activityTracker != null)
        {
            await _activityTracker.TrackActivityAsync(
                actionKey: ActionWeightRegistry.InventoryCreateWatch,
                category: ActionWeightRegistry.CategoryNewRecord,
                description: $"Creó Reloj Ref #{response.Reference} - {request.Description} (Caja #{request.CaseNumber})",
                weightScore: 10,
                entityName: "Item",
                entityReference: response.Reference.ToString(),
                cancellationToken: cancellationToken);
        }

        return Result.Success(response);
    }
}

public record DischargeItemCommand(
    int ItemId,
    DateTime SaleDate,
    int SaleValue) : ICommand<bool>;

public class DischargeItemCommandHandler : ICommandHandler<DischargeItemCommand, bool>
{
    private readonly IInventoryRepository _repository;
    private readonly IUserActivityTracker? _activityTracker;

    public DischargeItemCommandHandler(IInventoryRepository repository, IUserActivityTracker? activityTracker = null)
    {
        _repository = repository;
        _activityTracker = activityTracker;
    }

    public async Task<Result<bool>> Handle(DischargeItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetItemByIdAsync(request.ItemId, cancellationToken);
        if (item == null)
        {
            return Result.Failure<bool>(Error.NotFound("Items.NotFound", $"Artículo con ID {request.ItemId} no encontrado."));
        }

        item.Discharge(request.SaleDate, request.SaleValue);
        await _repository.SaveChangesAsync(cancellationToken);

        if (_activityTracker != null)
        {
            await _activityTracker.TrackActivityAsync(
                actionKey: ActionWeightRegistry.InventoryDischarge,
                category: ActionWeightRegistry.CategoryDischarge,
                description: $"Descargó por venta artículo Ref #{item.Reference} (${request.SaleValue:N0})",
                weightScore: 6,
                entityName: "Item",
                entityReference: item.Reference.ToString(),
                cancellationToken: cancellationToken);
        }

        return Result.Success(true);
    }
}

public record ToggleItemActiveCommand(int Reference, bool IsActive) : ICommand<bool>;

public class ToggleItemActiveCommandHandler : ICommandHandler<ToggleItemActiveCommand, bool>
{
    private readonly IInventoryRepository _repository;
    private readonly IUserActivityTracker? _activityTracker;

    public ToggleItemActiveCommandHandler(IInventoryRepository repository, IUserActivityTracker? activityTracker = null)
    {
        _repository = repository;
        _activityTracker = activityTracker;
    }

    public async Task<Result<bool>> Handle(ToggleItemActiveCommand request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetItemByReferenceAsync(request.Reference, cancellationToken);
        if (item == null)
        {
            return Result.Failure<bool>(Error.NotFound("Items.NotFound", $"Artículo con referencia #{request.Reference} no encontrado."));
        }

        item.Active = request.IsActive;
        if (request.IsActive && item.DateOfSale != null)
        {
            item.DateOfSale = null;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        if (_activityTracker != null)
        {
            await _activityTracker.TrackActivityAsync(
                actionKey: ActionWeightRegistry.InventoryToggleActive,
                category: ActionWeightRegistry.CategoryAuditQuick,
                description: $"{(request.IsActive ? "Activó" : "Desactivó")} artículo Ref #{request.Reference} en auditoría de catálogo",
                weightScore: 1,
                entityName: "Item",
                entityReference: request.Reference.ToString(),
                cancellationToken: cancellationToken);
        }

        return Result.Success(true);
    }
}

public record UpdateJewelItemCommand(
    int Reference,
    int ItemTypeId,
    string Description,
    double Weight,
    int GramValue,
    int CostValue,
    int? Length = null,
    int? ProviderId = null,
    string? ImageUrl = null) : ICommand<bool>;

public class UpdateJewelItemCommandValidator : AbstractValidator<UpdateJewelItemCommand>
{
    public UpdateJewelItemCommandValidator()
    {
        RuleFor(x => x.Reference).GreaterThan(0).WithMessage("La referencia es obligatoria.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("La descripción es obligatoria.");
        RuleFor(x => x.Weight).GreaterThan(0).WithMessage("El peso debe ser mayor a 0.");
        RuleFor(x => x.CostValue).GreaterThan(0).WithMessage("El costo debe ser mayor a 0.");
    }
}

public class UpdateJewelItemCommandHandler : ICommandHandler<UpdateJewelItemCommand, bool>
{
    private readonly IInventoryRepository _repository;
    private readonly IUserActivityTracker? _activityTracker;
    private readonly IInventoryImageService? _imageService;

    public UpdateJewelItemCommandHandler(
        IInventoryRepository repository,
        IUserActivityTracker? activityTracker = null,
        IInventoryImageService? imageService = null)
    {
        _repository = repository;
        _activityTracker = activityTracker;
        _imageService = imageService;
    }

    public async Task<Result<bool>> Handle(UpdateJewelItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetItemByReferenceAsync(request.Reference, cancellationToken);
        if (item == null)
        {
            return Result.Failure<bool>(Error.NotFound("Items.NotFound", $"Artículo con referencia #{request.Reference} no encontrado."));
        }

        item.ItemTypeId = request.ItemTypeId;
        item.Description = request.Description;
        item.CostValue = request.CostValue;
        item.ProviderId = request.ProviderId;

        if (item.ImageUrl != request.ImageUrl)
        {
            bool isRemoval = string.IsNullOrEmpty(request.ImageUrl) && !string.IsNullOrEmpty(item.ImageUrl);
            var oldImage = item.ImageUrl;
            item.ImageUrl = request.ImageUrl;
            item.ThumbnailUrl = request.ImageUrl;

            if (isRemoval)
            {
                if (!string.IsNullOrEmpty(oldImage) && _imageService != null)
                {
                    _imageService.DeleteImage(oldImage);
                }

                if (_activityTracker != null)
                {
                    await _activityTracker.TrackActivityAsync(
                        actionKey: ActionWeightRegistry.InventoryRemoveImage,
                        category: ActionWeightRegistry.CategoryUpdate,
                        description: $"Eliminó la fotografía de vitrina/catálogo del artículo Ref #{request.Reference}",
                        weightScore: 2,
                        entityName: "Item",
                        entityReference: request.Reference.ToString(),
                        cancellationToken: cancellationToken);
                }
            }
        }

        var jewel = await _repository.GetJewelByItemIdAsync(item.Id, cancellationToken);
        if (jewel != null)
        {
            jewel.Weight = request.Weight;
            jewel.GramValue = request.GramValue;
            jewel.Length = request.Length;
        }
        else
        {
            await _repository.AddJewelAsync(new Jewel(item.Id, request.Weight, request.GramValue, request.Length), cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        if (_activityTracker != null)
        {
            await _activityTracker.TrackActivityAsync(
                actionKey: ActionWeightRegistry.InventoryUpdateJewel,
                category: ActionWeightRegistry.CategoryUpdate,
                description: $"Actualizó Joya Ref #{request.Reference} - {request.Description}",
                weightScore: 4,
                entityName: "Item",
                entityReference: request.Reference.ToString(),
                cancellationToken: cancellationToken);
        }

        return Result.Success(true);
    }
}

public record UpdateWatchItemCommand(
    int Reference,
    int BrandId,
    string Model,
    string Description,
    string CaseNumber,
    GenderType GenderType,
    int CostValue,
    int? ProviderId = null,
    string? ImageUrl = null) : ICommand<bool>;

public class UpdateWatchItemCommandValidator : AbstractValidator<UpdateWatchItemCommand>
{
    public UpdateWatchItemCommandValidator()
    {
        RuleFor(x => x.Reference).GreaterThan(0).WithMessage("La referencia es obligatoria.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("La descripción es obligatoria.");
        RuleFor(x => x.BrandId).GreaterThan(0).WithMessage("La marca es obligatoria.");
        RuleFor(x => x.CostValue).GreaterThan(0).WithMessage("El costo debe ser mayor a 0.");
    }
}

public class UpdateWatchItemCommandHandler : ICommandHandler<UpdateWatchItemCommand, bool>
{
    private readonly IInventoryRepository _repository;
    private readonly IUserActivityTracker? _activityTracker;
    private readonly IInventoryImageService? _imageService;

    public UpdateWatchItemCommandHandler(
        IInventoryRepository repository,
        IUserActivityTracker? activityTracker = null,
        IInventoryImageService? imageService = null)
    {
        _repository = repository;
        _activityTracker = activityTracker;
        _imageService = imageService;
    }

    public async Task<Result<bool>> Handle(UpdateWatchItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetItemByReferenceAsync(request.Reference, cancellationToken);
        if (item == null)
        {
            return Result.Failure<bool>(Error.NotFound("Items.NotFound", $"Reloj con referencia #{request.Reference} no encontrado."));
        }

        item.Description = request.Description;
        item.CostValue = request.CostValue;
        item.ProviderId = request.ProviderId;

        if (item.ImageUrl != request.ImageUrl)
        {
            bool isRemoval = string.IsNullOrEmpty(request.ImageUrl) && !string.IsNullOrEmpty(item.ImageUrl);
            var oldImage = item.ImageUrl;
            item.ImageUrl = request.ImageUrl;
            item.ThumbnailUrl = request.ImageUrl;

            if (isRemoval)
            {
                if (!string.IsNullOrEmpty(oldImage) && _imageService != null)
                {
                    _imageService.DeleteImage(oldImage);
                }

                if (_activityTracker != null)
                {
                    await _activityTracker.TrackActivityAsync(
                        actionKey: ActionWeightRegistry.InventoryRemoveImage,
                        category: ActionWeightRegistry.CategoryUpdate,
                        description: $"Eliminó la fotografía de vitrina/catálogo del artículo Ref #{request.Reference}",
                        weightScore: 2,
                        entityName: "Item",
                        entityReference: request.Reference.ToString(),
                        cancellationToken: cancellationToken);
                }
            }
        }

        var watch = await _repository.GetWatchByItemIdAsync(item.Id, cancellationToken);
        if (watch != null)
        {
            watch.BrandId = request.BrandId;
            watch.Model = request.Model;
            watch.CaseNumber = request.CaseNumber;
            watch.GenderType = request.GenderType;
        }
        else
        {
            await _repository.AddWatchAsync(new Watch(item.Id, request.GenderType, request.CaseNumber, request.BrandId, request.Model), cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        if (_activityTracker != null)
        {
            await _activityTracker.TrackActivityAsync(
                actionKey: ActionWeightRegistry.InventoryUpdateWatch,
                category: ActionWeightRegistry.CategoryUpdate,
                description: $"Actualizó Reloj Ref #{request.Reference} - {request.Description} (Caja #{request.CaseNumber})",
                weightScore: 4,
                entityName: "Item",
                entityReference: request.Reference.ToString(),
                cancellationToken: cancellationToken);
        }

        return Result.Success(true);
    }
}

public record UpdateItemImageCommand(int Reference, string ImageUrl) : ICommand<bool>;

public class UpdateItemImageCommandValidator : AbstractValidator<UpdateItemImageCommand>
{
    public UpdateItemImageCommandValidator()
    {
        RuleFor(x => x.Reference).GreaterThan(0).WithMessage("La referencia es obligatoria.");
        RuleFor(x => x.ImageUrl).NotEmpty().WithMessage("La URL de la imagen es obligatoria.");
    }
}

public class UpdateItemImageCommandHandler : ICommandHandler<UpdateItemImageCommand, bool>
{
    private readonly IInventoryRepository _repository;
    private readonly IUserActivityTracker? _activityTracker;
    private readonly IInventoryImageService? _imageService;

    public UpdateItemImageCommandHandler(
        IInventoryRepository repository,
        IUserActivityTracker? activityTracker = null,
        IInventoryImageService? imageService = null)
    {
        _repository = repository;
        _activityTracker = activityTracker;
        _imageService = imageService;
    }

    public async Task<Result<bool>> Handle(UpdateItemImageCommand request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetItemByReferenceAsync(request.Reference, cancellationToken);
        if (item == null)
        {
            return Result.Failure<bool>(Error.NotFound("Items.NotFound", $"Artículo con referencia #{request.Reference} no encontrado."));
        }

        var oldImageUrl = item.ImageUrl;
        item.ImageUrl = request.ImageUrl;
        item.ThumbnailUrl = request.ImageUrl;

        await _repository.SaveChangesAsync(cancellationToken);

        // Si la foto anterior era local y cambió por una nueva, eliminar la anterior del almacenamiento
        if (!string.IsNullOrEmpty(oldImageUrl) && oldImageUrl != request.ImageUrl && _imageService != null)
        {
            _imageService.DeleteImage(oldImageUrl);
        }

        if (_activityTracker != null)
        {
            await _activityTracker.TrackActivityAsync(
                actionKey: ActionWeightRegistry.InventoryUpdateImage,
                category: ActionWeightRegistry.CategoryUpdate,
                description: $"Actualizó fotografía de artículo Ref #{request.Reference}",
                weightScore: 2,
                entityName: "Item",
                entityReference: request.Reference.ToString(),
                cancellationToken: cancellationToken);
        }

        return Result.Success(true);
    }
}

public record RemoveItemImageCommand(int Reference) : ICommand<bool>;

public class RemoveItemImageCommandValidator : AbstractValidator<RemoveItemImageCommand>
{
    public RemoveItemImageCommandValidator()
    {
        RuleFor(x => x.Reference).GreaterThan(0).WithMessage("La referencia es obligatoria.");
    }
}

public class RemoveItemImageCommandHandler : ICommandHandler<RemoveItemImageCommand, bool>
{
    private readonly IInventoryRepository _repository;
    private readonly IUserActivityTracker? _activityTracker;
    private readonly IInventoryImageService? _imageService;

    public RemoveItemImageCommandHandler(
        IInventoryRepository repository,
        IUserActivityTracker? activityTracker = null,
        IInventoryImageService? imageService = null)
    {
        _repository = repository;
        _activityTracker = activityTracker;
        _imageService = imageService;
    }

    public async Task<Result<bool>> Handle(RemoveItemImageCommand request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetItemByReferenceAsync(request.Reference, cancellationToken);
        if (item == null)
        {
            return Result.Failure<bool>(Error.NotFound("Items.NotFound", $"Artículo con referencia #{request.Reference} no encontrado."));
        }

        var oldImageUrl = item.ImageUrl;
        item.ImageUrl = null;
        item.ThumbnailUrl = null;

        await _repository.SaveChangesAsync(cancellationToken);

        // Eliminar archivo físico de almacenamiento si existía
        if (!string.IsNullOrEmpty(oldImageUrl) && _imageService != null)
        {
            _imageService.DeleteImage(oldImageUrl);
        }

        if (_activityTracker != null)
        {
            await _activityTracker.TrackActivityAsync(
                actionKey: ActionWeightRegistry.InventoryRemoveImage,
                category: ActionWeightRegistry.CategoryUpdate,
                description: $"Eliminó la fotografía de vitrina/catálogo del artículo Ref #{request.Reference}",
                weightScore: 2,
                entityName: "Item",
                entityReference: request.Reference.ToString(),
                cancellationToken: cancellationToken);
        }

        return Result.Success(true);
    }
}

public record DischargeItemByReferenceCommand(
    int Reference,
    DateTime SaleDate,
    int SaleValue) : ICommand<bool>;

public class DischargeItemByReferenceCommandHandler : ICommandHandler<DischargeItemByReferenceCommand, bool>
{
    private readonly IInventoryRepository _repository;
    private readonly IUserActivityTracker? _activityTracker;

    public DischargeItemByReferenceCommandHandler(IInventoryRepository repository, IUserActivityTracker? activityTracker = null)
    {
        _repository = repository;
        _activityTracker = activityTracker;
    }

    public async Task<Result<bool>> Handle(DischargeItemByReferenceCommand request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetItemByReferenceAsync(request.Reference, cancellationToken);
        if (item == null)
        {
            return Result.Failure<bool>(Error.NotFound("Items.NotFound", $"Artículo con referencia #{request.Reference} no encontrado en inventario."));
        }

        if (!item.Active)
        {
            return Result.Failure<bool>(Error.Conflict("Items.AlreadyDischarged", $"El artículo con referencia #{request.Reference} ya se encuentra inactivo/descargado con fecha {item.DateOfSale:dd/MM/yyyy}."));
        }

        item.Discharge(request.SaleDate, request.SaleValue);
        await _repository.SaveChangesAsync(cancellationToken);

        if (_activityTracker != null)
        {
            await _activityTracker.TrackActivityAsync(
                actionKey: ActionWeightRegistry.InventoryDischarge,
                category: ActionWeightRegistry.CategoryDischarge,
                description: $"Descargó por venta artículo Ref #{request.Reference} (${request.SaleValue:N0})",
                weightScore: 6,
                entityName: "Item",
                entityReference: request.Reference.ToString(),
                cancellationToken: cancellationToken);
        }

        return Result.Success(true);
    }
}
