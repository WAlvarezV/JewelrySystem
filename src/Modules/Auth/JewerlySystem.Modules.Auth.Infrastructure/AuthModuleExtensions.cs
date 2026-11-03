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
            ?? "Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN";

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(connectionString));

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

        services.AddHttpContextAccessor();

        // FIDO2 / Passkeys
        services.AddScoped<IFido2>(sp =>
        {
            var httpContext = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()?.HttpContext;
            var config = sp.GetRequiredService<IConfiguration>();

            var configuredDomain = config["Fido2:ServerDomain"];
            var configuredOrigin = config["Fido2:Origin"];

            // Detect Railway or public cloud environment
            var isRailway = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT"))
                || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_PROJECT_ID"))
                || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_STATIC_URL"))
                || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAILWAY_PUBLIC_DOMAIN"));

            var railwayDomain = Environment.GetEnvironmentVariable("RAILWAY_PUBLIC_DOMAIN")
                ?? Environment.GetEnvironmentVariable("RAILWAY_STATIC_URL");

            string serverDomain = "localhost";
            var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "https://localhost:5001",
                "http://localhost:5000",
                "https://localhost",
                "http://localhost",
                "https://127.0.0.1:5001",
                "http://127.0.0.1:5000",
                "https://jewelrysystem-production.up.railway.app",
                "http://jewelrysystem-production.up.railway.app"
            };

            if (!string.IsNullOrWhiteSpace(railwayDomain))
            {
                var clean = railwayDomain.Trim().TrimEnd('/');
                origins.Add($"https://{clean}");
                origins.Add($"http://{clean}");
                serverDomain = clean;
            }

            if (!string.IsNullOrWhiteSpace(configuredOrigin))
            {
                origins.Add(configuredOrigin.Trim().TrimEnd('/'));
            }

            if (httpContext != null)
            {
                var request = httpContext.Request;
                var forwardedHost = request.Headers["X-Forwarded-Host"].FirstOrDefault();
                var hostWithPort = !string.IsNullOrWhiteSpace(forwardedHost) ? forwardedHost : (request.Host.Value ?? "localhost");
                var hostName = (hostWithPort.Split(':')[0] ?? "localhost").Trim();

                var scheme = request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? request.Scheme;
                if (string.IsNullOrWhiteSpace(scheme)) scheme = "https";

                origins.Add($"{scheme}://{hostWithPort}");
                origins.Add($"{scheme}://{hostName}");
                origins.Add($"https://{hostName}");
                origins.Add($"http://{hostName}");

                // If running on a non-IP, non-localhost host (e.g. railway or custom domain)
                if (!System.Net.IPAddress.TryParse(hostName, out _) && hostName != "localhost" && hostName != "0.0.0.0")
                {
                    serverDomain = hostName;
                }
            }
            else if (isRailway)
            {
                serverDomain = !string.IsNullOrWhiteSpace(railwayDomain)
                    ? railwayDomain.Trim().TrimEnd('/')
                    : "jewelrysystem-production.up.railway.app";
            }

            // Explicit override if configured in appsettings and not "localhost"
            if (!string.IsNullOrWhiteSpace(configuredDomain) && configuredDomain != "localhost")
            {
                serverDomain = configuredDomain.Trim();
            }

            return new Fido2(new Fido2Configuration
            {
                RPID = serverDomain,
                RPName = "JewerlySystem",
                Origins = origins
            });
        });

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
