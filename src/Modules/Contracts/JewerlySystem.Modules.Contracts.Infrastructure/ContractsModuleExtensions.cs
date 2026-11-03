using FluentValidation;
using JewerlySystem.Common.Application;
using JewerlySystem.Modules.Contracts.Application;
using JewerlySystem.Modules.Contracts.Application.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JewerlySystem.Modules.Contracts.Infrastructure;

public static class ContractsModuleExtensions
{
    public static IServiceCollection AddContractsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PomonaCs")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN";

        services.AddDbContext<ContractsDbContext>((sp, options) =>
        {
            var tenantConn = sp.GetService<JewerlySystem.Common.Application.ITenantConnectionProvider>();
            var cs = tenantConn?.GetCurrentConnectionString() ?? connectionString;
            options.UseNpgsql(cs);
        });

        services.AddScoped<IContractRepository, ContractRepository>();

        // MediatR & Validators
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateContractCommand).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(CreateContractCommandValidator).Assembly);

        return services;
    }
}
