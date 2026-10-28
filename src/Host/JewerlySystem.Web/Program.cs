using System.Security.Claims;
using JewerlySystem.Common.Infrastructure;
using JewerlySystem.Modules.Auth.Application;
using JewerlySystem.Modules.Auth.Infrastructure;
using JewerlySystem.Modules.Cash.Infrastructure;
using JewerlySystem.Modules.Contracts.Infrastructure;
using JewerlySystem.Modules.Customers.Infrastructure;
using JewerlySystem.Modules.Inventory.Infrastructure;
using JewerlySystem.Modules.Invoicing.Infrastructure;
using JewerlySystem.Web.Components;
using JewerlySystem.Web.Data;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PomonaCs")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=PomonaDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

// Shared DB context for host
builder.Services.AddScoped<AuditInterceptor>();
builder.Services.AddDbContext<JewerlyDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
    options.AddInterceptors(auditInterceptor);
});

// Register Modules
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddContractsModule(builder.Configuration);
builder.Services.AddCustomersModule(builder.Configuration);
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddCashModule(builder.Configuration);
builder.Services.AddInvoicingModule(builder.Configuration);

// Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Blazor Interactive Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Passkeys WebAuthn Endpoints
app.MapPost("/api/auth/passkey/assertion-options", async (ISender sender, string? username) =>
{
    var result = await sender.Send(new GetPasskeyAssertionOptionsQuery(username));
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});

app.MapPost("/api/auth/passkey/verify-assertion", async (HttpContext httpContext, ISender sender, string clientResponseJson) =>
{
    var result = await sender.Send(new VerifyPasskeyAssertionCommand(clientResponseJson));
    if (result.IsFailure)
    {
        return Results.BadRequest(result.Error);
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, "Usuario"),
        new(ClaimTypes.AuthenticationMethod, "Passkey")
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    return Results.Ok(new { Succeeded = true });
});

app.MapPost("/api/auth/passkey/creation-options", async (ISender sender, string userId, string displayName) =>
{
    var result = await sender.Send(new GetPasskeyCreationOptionsQuery(userId, displayName));
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});

app.MapPost("/api/auth/passkey/register", async (ISender sender, string userId, string keyName, string clientResponseJson) =>
{
    var result = await sender.Send(new RegisterPasskeyCommand(userId, keyName, clientResponseJson));
    return result.IsSuccess ? Results.Ok(new { Succeeded = true }) : Results.BadRequest(result.Error);
});

app.MapPost("/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

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

app.Run();
