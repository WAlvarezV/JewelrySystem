using JewerlySystem.Web.Extensions;
using Microsoft.AspNetCore.HttpOverrides;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Host Infrastructure (Database, Emailing, AI Voice Intelligence)
builder.Services.AddHostInfrastructure(builder.Configuration);

// 2. Business Modules (Auth, Contracts, Inventory, Cash, Customers, Invoicing)
builder.Services.AddAppModules(builder.Configuration);

// 3. Security & Authentication (Cookies, MFA Schemes, Passkeys)
builder.Services.AddAppSecurity();

// 4. Presentation & Culture (Blazor Server, AntDesign & Colombia Localization COP)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAntDesign();
builder.Services.AddColombiaLocalization();

var app = builder.Build();

// Configure Middleware Pipeline
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.All
});

app.UseColombiaLocalization();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

// Map Endpoints & Interactive Blazor Components
app.MapAuthEndpoints();
app.MapAppRazorComponents();

// Database Initialization & Startup Seeding
await app.InitializeDatabaseAsync();

app.Run();
