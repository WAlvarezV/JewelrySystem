using FluentValidation;
using JewerlySystem.Common.Application;
using JewerlySystem.Modules.Cash.Application;
using JewerlySystem.Modules.Cash.Application.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JewerlySystem.Modules.Cash.Infrastructure;

public static class CashModuleExtensions
{
    public static IServiceCollection AddCashModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PomonaCs")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=PomonaDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<CashDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ICashRepository, CashRepository>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateDailyRecordCommand).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(CreateDailyRecordCommandValidator).Assembly);

        return services;
    }
}
