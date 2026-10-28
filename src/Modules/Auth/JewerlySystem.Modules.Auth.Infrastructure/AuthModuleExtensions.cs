using System.Text.Encodings.Web;
using Fido2NetLib;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JewerlySystem.Modules.Auth.Infrastructure;

public static class AuthModuleExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PomonaCs")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=PomonaDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AuthDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        // FIDO2 / Passkeys
        services.AddScoped<IFido2>(sp => new Fido2(new Fido2Configuration
        {
            RPID = configuration["Fido2:ServerDomain"] ?? "localhost",
            RPName = "JewerlySystem",
            Origins = new HashSet<string>
            {
                configuration["Fido2:Origin"] ?? "https://localhost:5001",
                "http://localhost:5000"
            }
        }));

        services.AddScoped<IFido2Service, Fido2Service>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddSingleton(UrlEncoder.Default);

        // MediatR Handlers from this assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(AuthModuleExtensions).Assembly);
        });

        return services;
    }
}
