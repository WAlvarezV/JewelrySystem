using FluentValidation;
using JewerlySystem.Common.Application;
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
    int? ProviderId = null) : ICommand<CreateItemResponse>;

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

    public CreateJewelItemCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CreateItemResponse>> Handle(CreateJewelItemCommand request, CancellationToken cancellationToken)
    {
        var item = new Item(
            reference: 0,
            costValue: request.CostValue,
            dateOfEntry: request.DateOfEntry,
            description: request.Description,
            itemTypeId: request.ItemTypeId,
            providerId: request.ProviderId
        );

        var jewel = new Jewel(
            itemId: 0,
            weight: request.Weight,
            gramValue: request.GramValue,
            length: request.Length
        );

        var response = await _repository.CreateJewelItemAtomicAsync(item, jewel, cancellationToken);
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
    int BrandId,
    int? ProviderId = null) : ICommand<CreateItemResponse>;

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

    public CreateWatchItemCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CreateItemResponse>> Handle(CreateWatchItemCommand request, CancellationToken cancellationToken)
    {
        var item = new Item(
            reference: 0,
            costValue: request.CostValue,
            dateOfEntry: request.DateOfEntry,
            description: request.Description,
            itemTypeId: request.ItemTypeId,
            providerId: request.ProviderId
        );

        var watch = new Watch(
            itemId: 0,
            genderType: request.GenderType,
            caseNumber: request.CaseNumber,
            brandId: request.BrandId
        );

        var response = await _repository.CreateWatchItemAtomicAsync(item, watch, cancellationToken);
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

    public DischargeItemCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
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

        return Result.Success(true);
    }
}

public record ToggleItemActiveCommand(int Reference, bool IsActive) : ICommand<bool>;

public class ToggleItemActiveCommandHandler : ICommandHandler<ToggleItemActiveCommand, bool>
{
    private readonly IInventoryRepository _repository;

    public ToggleItemActiveCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
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
    int? ProviderId = null) : ICommand<bool>;

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

    public UpdateJewelItemCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
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
        return Result.Success(true);
    }
}

public record UpdateWatchItemCommand(
    int Reference,
    int BrandId,
    string Description,
    string CaseNumber,
    GenderType GenderType,
    int CostValue,
    int? ProviderId = null) : ICommand<bool>;

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

    public UpdateWatchItemCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
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

        var watch = await _repository.GetWatchByItemIdAsync(item.Id, cancellationToken);
        if (watch != null)
        {
            watch.BrandId = request.BrandId;
            watch.CaseNumber = request.CaseNumber;
            watch.GenderType = request.GenderType;
        }
        else
        {
            await _repository.AddWatchAsync(new Watch(item.Id, request.GenderType, request.CaseNumber, request.BrandId), cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
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

    public DischargeItemByReferenceCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
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
        return Result.Success(true);
    }
}
