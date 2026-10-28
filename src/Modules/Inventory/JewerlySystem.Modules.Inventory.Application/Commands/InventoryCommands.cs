using FluentValidation;
using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
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
    int? ProviderId = null) : ICommand<int>;

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

public class CreateJewelItemCommandHandler : ICommandHandler<CreateJewelItemCommand, int>
{
    private readonly IInventoryRepository _repository;

    public CreateJewelItemCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(CreateJewelItemCommand request, CancellationToken cancellationToken)
    {
        var nextRef = await _repository.GetNextItemReferenceAsync(cancellationToken);

        var item = new Item(
            reference: nextRef,
            costValue: request.CostValue,
            dateOfEntry: request.DateOfEntry,
            description: request.Description,
            itemTypeId: request.ItemTypeId,
            providerId: request.ProviderId
        );

        await _repository.AddItemAsync(item, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var jewel = new Jewel(
            itemId: item.Id,
            weight: request.Weight,
            gramValue: request.GramValue,
            length: request.Length
        );

        await _repository.AddJewelAsync(jewel, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(item.Id);
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
    int? ProviderId = null) : ICommand<int>;

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

public class CreateWatchItemCommandHandler : ICommandHandler<CreateWatchItemCommand, int>
{
    private readonly IInventoryRepository _repository;

    public CreateWatchItemCommandHandler(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(CreateWatchItemCommand request, CancellationToken cancellationToken)
    {
        var nextRef = await _repository.GetNextItemReferenceAsync(cancellationToken);

        var item = new Item(
            reference: nextRef,
            costValue: request.CostValue,
            dateOfEntry: request.DateOfEntry,
            description: request.Description,
            itemTypeId: request.ItemTypeId,
            providerId: request.ProviderId
        );

        await _repository.AddItemAsync(item, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var watch = new Watch(
            itemId: item.Id,
            genderType: request.GenderType,
            caseNumber: request.CaseNumber,
            brandId: request.BrandId
        );

        await _repository.AddWatchAsync(watch, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(item.Id);
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
