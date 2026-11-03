using FluentValidation;
using JewerlySystem.Common.Application;
using JewerlySystem.Modules.Inventory.Application;
using JewerlySystem.Modules.Inventory.Application.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JewerlySystem.Modules.Inventory.Infrastructure;

public static class InventoryModuleExtensions
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PomonaCs")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN";

        services.AddDbContext<InventoryDbContext>((sp, options) =>
        {
            var tenantConn = sp.GetService<JewerlySystem.Common.Application.ITenantConnectionProvider>();
            var cs = tenantConn?.GetCurrentConnectionString() ?? connectionString;
            options.UseNpgsql(cs);
        });

        services.AddScoped<IInventoryRepository, InventoryRepository>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateJewelItemCommand).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(CreateJewelItemCommandValidator).Assembly);

        return services;
    }
}
