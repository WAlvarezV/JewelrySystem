using JewerlySystem.Web.Endpoints;
using JewerlySystem.Web.Extensions;
using Microsoft.AspNetCore.HttpOverrides;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Hardening de Kestrel: Ocultar banner de servidor (OWASP A02 / ISO 27001 A.8.20)
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// 1. Host Infrastructure (Database, Emailing, AI Voice Intelligence)
builder.Services.AddHostInfrastructure(builder.Configuration);

// 2. Business Modules (Auth, Contracts, Inventory, Cash, Customers, Invoicing)
builder.Services.AddAppModules(builder.Configuration);

// 3. Security & Authentication (Cookies, MFA Schemes, Passkeys, Rate Limiting, Security Policies)
builder.Services.AddAppSecurity();

// 4. Presentation & Culture (Blazor Server, AntDesign & Colombia Localization COP)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    });
builder.Services.AddAntDesign();
builder.Services.AddColombiaLocalization();

var app = builder.Build();

// Configure Middleware Pipeline
// Hardening de ForwardedHeaders: Evitar Spoofing de cabeceras arbitrarias (OWASP A01 / A02)
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Aplicar Cabeceras de Seguridad HTTP estrictas (CSP, HSTS, X-Frame-Options, etc.)
app.UseSecurityHeaders();

app.UseColombiaLocalization();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

// Aplicar Rate Limiter (Protección DoS y Fuerza Bruta)
app.UseRateLimiter();

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

// Map Endpoints & Interactive Blazor Components
app.MapAuthEndpoints();
app.MapInventoryImageEndpoints();
app.MapAppRazorComponents();

// Database Initialization & Startup Seeding
await app.InitializeDatabaseAsync();

app.Run();
