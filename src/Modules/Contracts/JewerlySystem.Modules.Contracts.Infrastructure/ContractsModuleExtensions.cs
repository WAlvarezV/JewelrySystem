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
            ?? "Server=localhost;Database=PomonaDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<ContractsDbContext>(options =>
            options.UseSqlServer(connectionString));

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
