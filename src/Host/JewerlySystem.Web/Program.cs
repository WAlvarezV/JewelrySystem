using System.Security.Claims;
using JewerlySystem.Common.Application.Voice;
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
using JewerlySystem.Web.Services.Voice;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PomonaCs")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN";

// Shared DB context for host
builder.Services.AddScoped<AuditInterceptor>();
builder.Services.AddDbContext<JewerlyDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
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

// Voice AI Intelligence & Operations
builder.Services.AddVoiceIntelligence();
builder.Services.AddScoped<IVoiceCommandDispatcher, VoiceCommandDispatcher>();

// Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Blazor Interactive Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntDesign();

var app = builder.Build();

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

// Standard HTTP POST Login Endpoint to issue authentication cookie
app.MapPost("/api/auth/login", async (
    HttpContext httpContext,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    string username = form["username"].ToString().Trim();
    string password = form["password"].ToString();
    string returnUrl = form["returnUrl"].ToString();

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/?error=" + Uri.EscapeDataString("Por favor ingresa usuario y contraseña."));
    }

    var user = await userManager.FindByNameAsync(username) ?? await userManager.FindByEmailAsync(username);
    if (user == null)
    {
        return Results.Redirect("/?error=" + Uri.EscapeDataString("Usuario o contraseña incorrectos."));
    }

    var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
    if (!result.Succeeded)
    {
        if (result.IsLockedOut)
        {
            return Results.Redirect("/?error=" + Uri.EscapeDataString("Cuenta bloqueada temporalmente por intentos fallidos."));
        }
        return Results.Redirect("/?error=" + Uri.EscapeDataString("Usuario o contraseña incorrectos."));
    }

    var roles = await userManager.GetRolesAsync(user);
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Name, user.UserName ?? user.Email ?? "Usuario"),
        new(ClaimTypes.Email, user.Email ?? "")
    };
    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
    });

    user.LastLoginAt = DateTime.UtcNow;
    await userManager.UpdateAsync(user);

    var destination = string.IsNullOrWhiteSpace(returnUrl) ? "/dashboard" : returnUrl;
    return Results.Redirect(destination);
}).DisableAntiforgery();

// Passkeys WebAuthn Endpoints
app.MapPost("/api/auth/passkey/assertion-options", async (ISender sender, string? username) =>
{
    var result = await sender.Send(new GetPasskeyAssertionOptionsQuery(username));
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
});

app.MapPost("/api/auth/passkey/verify-assertion", async (HttpContext httpContext, ISender sender, UserManager<ApplicationUser> userManager, string clientResponseJson) =>
{
    var result = await sender.Send(new VerifyPasskeyAssertionCommand(clientResponseJson));
    if (result.IsFailure)
    {
        return Results.BadRequest(result.Error);
    }

    const string adminEmail = "walvarez@ingnova.team";
    var user = await userManager.FindByEmailAsync(adminEmail);
    var roles = user != null ? await userManager.GetRolesAsync(user) : new List<string> { "Admin", "Administrador" };

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user?.Id ?? Guid.NewGuid().ToString()),
        new(ClaimTypes.Name, user?.UserName ?? adminEmail),
        new(ClaimTypes.Email, user?.Email ?? adminEmail),
        new(ClaimTypes.AuthenticationMethod, "Passkey")
    };
    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
    });

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
    return Results.Redirect("/");
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

// Ensure database and schema are created, then seed navigation, Admin role and user walvarez@ingnova.team
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<JewerlyDbContext>();
    await db.Database.EnsureCreatedAsync();

    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
    await sender.Send(new GetNavigationModulesQuery());
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Could not run initial database migration and seeding on startup.");
}

app.Run();
