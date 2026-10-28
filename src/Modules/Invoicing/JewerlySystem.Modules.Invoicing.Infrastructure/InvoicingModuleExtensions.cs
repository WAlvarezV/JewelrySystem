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
            ?? "Server=localhost;Database=PomonaDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<InvoicingDbContext>(options =>
            options.UseSqlServer(connectionString));

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
