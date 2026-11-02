using FluentValidation;
using JewerlySystem.Common.Application;
using JewerlySystem.Modules.Invoicing.Application;
using JewerlySystem.Modules.Invoicing.Application.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JewerlySystem.Modules.Invoicing.Infrastructure;

public static class InvoicingModuleExtensions
{
    public static IServiceCollection AddInvoicingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PomonaCs")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN";

        services.AddDbContext<InvoicingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IInvoicingRepository, InvoicingRepository>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateInvoiceCommand).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(CreateInvoiceCommandValidator).Assembly);

        return services;
    }
}
