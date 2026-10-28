using FluentValidation;
using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Cash.Domain;

namespace JewerlySystem.Modules.Cash.Application.Commands;

public record CreateDailyRecordCommand(
    DateTime Date,
    int? Reference,
    string Description,
    int Value,
    RecordType RecordType,
    PaymentMethod PaymentMethod,
    string? Number = null) : ICommand<int>;

public class CreateDailyRecordCommandValidator : AbstractValidator<CreateDailyRecordCommand>
{
    public CreateDailyRecordCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().WithMessage("La descripción del registro de caja es obligatoria.");
        RuleFor(x => x.Value).GreaterThan(0).WithMessage("El valor debe ser mayor a 0.");
    }
}

public class CreateDailyRecordCommandHandler : ICommandHandler<CreateDailyRecordCommand, int>
{
    private readonly ICashRepository _repository;

    public CreateDailyRecordCommandHandler(ICashRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(CreateDailyRecordCommand request, CancellationToken cancellationToken)
    {
        var recordNumber = !string.IsNullOrWhiteSpace(request.Number)
            ? request.Number
            : (await _repository.GetNextRecordNumberAsync(cancellationToken)).ToString();

        var record = new DailyRecord(
            date: request.Date,
            number: recordNumber,
            reference: request.Reference,
            description: request.Description,
            value: request.Value,
            recordType: request.RecordType,
            paymentMethod: request.PaymentMethod
        );

        await _repository.AddAsync(record, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(record.Id);
    }
}
