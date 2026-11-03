using FluentValidation;
using JewerlySystem.Common.Application;
using JewerlySystem.Modules.Customers.Application;
using JewerlySystem.Modules.Customers.Application.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JewerlySystem.Modules.Customers.Infrastructure;

public static class CustomersModuleExtensions
{
    public static IServiceCollection AddCustomersModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PomonaCs")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN";

        services.AddDbContext<CustomersDbContext>((sp, options) =>
        {
            var tenantConn = sp.GetService<JewerlySystem.Common.Application.ITenantConnectionProvider>();
            var cs = tenantConn?.GetCurrentConnectionString() ?? connectionString;
            options.UseNpgsql(cs);
        });

        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateCustomerCommand).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(CreateCustomerCommandValidator).Assembly);

        return services;
    }
}
