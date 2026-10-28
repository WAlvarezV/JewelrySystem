using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Invoicing.Application.DTOs;

namespace JewerlySystem.Modules.Invoicing.Application.Queries;

public record GetInvoiceByIdQuery(int Id) : IQuery<InvoiceDto>;

public class GetInvoiceByIdQueryHandler : IQueryHandler<GetInvoiceByIdQuery, InvoiceDto>
{
    private readonly IInvoicingRepository _repository;

    public GetInvoiceByIdQueryHandler(IInvoicingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<InvoiceDto>> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (invoice == null)
        {
            return Result.Failure<InvoiceDto>(Error.NotFound("Invoices.NotFound", $"Factura con ID {request.Id} no encontrada."));
        }

        return Result.Success(invoice.ToDto());
    }
}

public record GetInvoicesListQuery(int? ProviderId = null) : IQuery<List<InvoiceDto>>;

public class GetInvoicesListQueryHandler : IQueryHandler<GetInvoicesListQuery, List<InvoiceDto>>
{
    private readonly IInvoicingRepository _repository;

    public GetInvoicesListQueryHandler(IInvoicingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<InvoiceDto>>> Handle(GetInvoicesListQuery request, CancellationToken cancellationToken)
    {
        var invoices = await _repository.ListAsync(request.ProviderId, cancellationToken);
        var dtos = invoices.Select(i => i.ToDto()).ToList();

        return Result.Success(dtos);
    }
}

public record GetInvoicePaymentsQuery(int InvoiceId) : IQuery<List<InvoicePaymentDto>>;

public class GetInvoicePaymentsQueryHandler : IQueryHandler<GetInvoicePaymentsQuery, List<InvoicePaymentDto>>
{
    private readonly IInvoicingRepository _repository;

    public GetInvoicePaymentsQueryHandler(IInvoicingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<InvoicePaymentDto>>> Handle(GetInvoicePaymentsQuery request, CancellationToken cancellationToken)
    {
        var payments = await _repository.GetPaymentsByInvoiceIdAsync(request.InvoiceId, cancellationToken);
        var dtos = payments.Select(p => new InvoicePaymentDto(
            p.Id,
            p.Number,
            p.Date,
            p.Value,
            p.EntityId,
            p.PaymentMethod
        )).ToList();

        return Result.Success(dtos);
    }
}
