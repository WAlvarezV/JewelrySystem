using JewerlySystem.Common.Application;
using JewerlySystem.Common.Application.Documentation.Abstractions;
using JewerlySystem.Common.Application.Emailing;
using JewerlySystem.Common.Application.Voice;
using JewerlySystem.Common.Infrastructure;
using JewerlySystem.Common.Infrastructure.Documentation;
using JewerlySystem.Common.Infrastructure.Emailing;
using JewerlySystem.Common.Presentation.Services;
using JewerlySystem.Modules.Auth.Infrastructure;
using JewerlySystem.Modules.Cash.Infrastructure;
using JewerlySystem.Modules.Contracts.Infrastructure;
using JewerlySystem.Modules.Customers.Infrastructure;
using JewerlySystem.Modules.Inventory.Infrastructure;
using JewerlySystem.Modules.Invoicing.Infrastructure;
using JewerlySystem.Web.Components;
using JewerlySystem.Web.Data;
using JewerlySystem.Web.Services;
using JewerlySystem.Web.Services.Voice;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using JewerlySystem.Common.Infrastructure.Tenant;
using JewerlySystem.Web.Tenants;

namespace JewerlySystem.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHostInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var rawCs = configuration.GetConnectionString("PomonaCs");
        if (string.IsNullOrWhiteSpace(rawCs))
            rawCs = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(rawCs))
            rawCs = configuration["DATABASE_URL"] ?? Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(rawCs))
            rawCs = "Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN";

        var connectionString = TenantRegistry.NormalizeNpgsqlConnectionString(rawCs);

        // Multi-Tenant Infrastructure
        services.AddSingleton<ITenantRegistry, TenantRegistry>();
        services.AddScoped<WebTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<WebTenantContext>());
        services.AddScoped<ITenantConnectionProvider>(sp => sp.GetRequiredService<WebTenantContext>());

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUserActivityTracker, UserActivityService>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(UserActivityService).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly);
        });
        services.AddScoped<AuditInterceptor>();

        // Documentation & Interactive User Manuals
        services.AddScoped<IDocumentationProvider, FileDocumentationProvider>();
        services.AddSingleton<HelpRouteRegistry>();
        services.AddSingleton<IHelpRouteRegistry>(sp => sp.GetRequiredService<HelpRouteRegistry>());
        services.AddDbContext<JewerlyDbContext>((sp, options) =>
        {
            var tenantConn = sp.GetService<ITenantConnectionProvider>();
            var cs = tenantConn?.GetCurrentConnectionString() ?? connectionString;
            options.UseNpgsql(cs);
            var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
            options.AddInterceptors(auditInterceptor);
        });

        // Emailing & Templates
        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
        services.AddHttpClient<IEmailSender, ResendEmailSender>();

        // Voice AI Intelligence & Operations
        services.AddVoiceIntelligence();
        services.AddScoped<IVoiceCommandDispatcher, VoiceCommandDispatcher>();

        return services;
    }

    public static IServiceCollection AddAppModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthModule(configuration);
        services.AddContractsModule(configuration);
        services.AddCustomersModule(configuration);
        services.AddInventoryModule(configuration);
        services.AddCashModule(configuration);
        services.AddInvoicingModule(configuration);

        return services;
    }

    public static IServiceCollection AddAppSecurity(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = "JewerlySystem.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.LoginPath = "/";
            options.LogoutPath = "/logout";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        })
        .AddCookie(IdentityConstants.TwoFactorUserIdScheme, options =>
        {
            options.Cookie.Name = "JewerlySystem.2FA";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        });

        // Configuración de HSTS estricto (ISO 27001 A.8.24 / SOC 2 CC6.7 / OWASP A02)
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });

        // Antiforgery Cookie Hardening
        services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "JewerlySystem.XSRF";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.HeaderName = "X-XSRF-TOKEN";
        });

        // Hardening de Rate Limiting y ProblemDetails
        services.AddSecurityHardening();

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        return services;
    }

    public static IEndpointRouteBuilder MapAppRazorComponents(this WebApplication app)
    {
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(
                typeof(JewerlySystem.Modules.Auth.Presentation.Pages.Login).Assembly,
                typeof(JewerlySystem.Modules.Contracts.Presentation.Pages.ContractsList).Assembly,
                typeof(JewerlySystem.Modules.Inventory.Presentation.Pages.InventoryList).Assembly,
                typeof(JewerlySystem.Modules.Cash.Presentation.Pages.CashDaily).Assembly,
                typeof(JewerlySystem.Modules.Customers.Presentation.Pages.CustomersList).Assembly,
                typeof(JewerlySystem.Modules.Invoicing.Presentation.Pages.InvoicingList).Assembly
            );

        return app;
    }
}
