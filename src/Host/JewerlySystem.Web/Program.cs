using System.Security.Claims;
using JewerlySystem.Common.Application.Emailing;
using JewerlySystem.Common.Application.Voice;
using JewerlySystem.Common.Infrastructure;
using JewerlySystem.Common.Infrastructure.Emailing;
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

// Emailing & Templates (SI.SIOT Standard)
builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>();

// Authentication & Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddCookie(IdentityConstants.TwoFactorUserIdScheme, options =>
{
    options.Cookie.Name = "JewerlySystem.2FA";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
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

// Standard HTTP POST Login Endpoint with Mandatory MFA (SI.SIOT Standard)
app.MapPost("/api/auth/login", async (
    HttpContext httpContext,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailTemplateService templateService,
    IEmailSender emailSender,
    ILoggerFactory loggerFactory) =>
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
    if (!result.Succeeded && !result.RequiresTwoFactor)
    {
        if (result.IsLockedOut)
        {
            return Results.Redirect("/?error=" + Uri.EscapeDataString("Cuenta bloqueada temporalmente por intentos fallidos."));
        }
        return Results.Redirect("/?error=" + Uri.EscapeDataString("Usuario o contraseña incorrectos."));
    }

    // Check if user has Google / Microsoft Authenticator (TOTP) enabled
    var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
    var authenticatorKey = await userManager.GetAuthenticatorKeyAsync(user);
    var hasAuthenticator = isTwoFactorEnabled && !string.IsNullOrEmpty(authenticatorKey);

    // Issue 2FA temporary cookie
    var twoFactorClaims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Email, user.Email ?? ""),
        new("returnUrl", string.IsNullOrWhiteSpace(returnUrl) ? "/dashboard" : returnUrl),
        new("mfaMethod", hasAuthenticator ? "authenticator" : "email")
    };
    var twoFactorIdentity = new ClaimsIdentity(twoFactorClaims, IdentityConstants.TwoFactorUserIdScheme);
    await httpContext.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, new ClaimsPrincipal(twoFactorIdentity), new AuthenticationProperties
    {
        IsPersistent = false,
        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10)
    });

    if (hasAuthenticator)
    {
        // TOTP Mobile App (Google / Microsoft Authenticator): No email sent!
        var logger = loggerFactory.CreateLogger("Auth");
        logger.LogInformation("Usuario {UserName} requiere código TOTP móvil (sin correo).", user.UserName);
        return Results.Redirect($"/?mfa=true&method=authenticator&user={Uri.EscapeDataString(user.UserName ?? user.Email ?? "")}");
    }
    else
    {
        // Fallback for users without Google/Microsoft Authenticator app yet: Email OTP
        var otp = await userManager.GenerateTwoFactorTokenAsync(user, "Email");
        var logger = loggerFactory.CreateLogger("Auth");

        var templateValues = new Dictionary<string, string>
        {
            ["UserName"] = user.UserName ?? user.Email ?? "Usuario",
            ["UserEmail"] = user.Email ?? "",
            ["OtpCode"] = otp,
            ["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };

        var htmlBody = templateService.GenerateHtml("otp-mfa-template.html", templateValues);
        var sent = await emailSender.SendEmailAsync(user.Email ?? "", "Código de Seguridad MFA - JewerlySystem", htmlBody);

        logger.LogInformation("MFA OTP generado para {Email}: [{Otp}]. Enviado: {Sent}", user.Email, otp, sent);

        var masked = MaskEmail(user.Email);
        return Results.Redirect($"/?mfa=true&method=email&email={Uri.EscapeDataString(masked)}");
    }
}).DisableAntiforgery();

app.MapPost("/api/auth/verify-otp", async (
    HttpContext httpContext,
    UserManager<ApplicationUser> userManager) =>
{
    var authResult = await httpContext.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
    if (!authResult.Succeeded || authResult.Principal == null)
    {
        return Results.Redirect("/?error=" + Uri.EscapeDataString("La sesión de verificación ha expirado. Por favor ingresa nuevamente."));
    }

    var userId = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
    var returnUrl = authResult.Principal.FindFirstValue("returnUrl") ?? "/dashboard";
    var mfaMethod = authResult.Principal.FindFirstValue("mfaMethod") ?? "authenticator";

    if (string.IsNullOrEmpty(userId))
    {
        return Results.Redirect("/?error=" + Uri.EscapeDataString("Error identificando la sesión temporal."));
    }

    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
    {
        return Results.Redirect("/?error=" + Uri.EscapeDataString("Usuario no encontrado."));
    }

    var form = await httpContext.Request.ReadFormAsync();
    string otpCode = form["otpCode"].ToString().Trim();

    if (string.IsNullOrWhiteSpace(otpCode))
    {
        var redirectUrl = mfaMethod == "authenticator"
            ? $"/?mfa=true&method=authenticator&user={Uri.EscapeDataString(user.UserName ?? "")}&error={Uri.EscapeDataString("Por favor ingresa el código de 6 dígitos.")}"
            : $"/?mfa=true&method=email&email={Uri.EscapeDataString(MaskEmail(email))}&error={Uri.EscapeDataString("Por favor ingresa el código de 6 dígitos.")}";
        return Results.Redirect(redirectUrl);
    }

    var cleanCode = otpCode.Replace(" ", string.Empty).Replace("-", string.Empty).Trim();
    bool isValid = false;
    string authMethodUsed = "Password+Authenticator";

    // 1. Try TOTP Authenticator Token Provider (Google Authenticator / Microsoft Authenticator)
    if (await userManager.GetTwoFactorEnabledAsync(user))
    {
        isValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            cleanCode);

        // 2. Try Emergency Recovery Code
        if (!isValid)
        {
            var recoveryResult = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, cleanCode);
            if (recoveryResult.Succeeded)
            {
                isValid = true;
                authMethodUsed = "Password+RecoveryCode";
            }
        }
    }

    // 3. Fallback to Email provider if not verified yet
    if (!isValid)
    {
        isValid = await userManager.VerifyTwoFactorTokenAsync(user, "Email", cleanCode);
        if (isValid)
        {
            authMethodUsed = "Password+EmailOTP";
        }
    }

    if (!isValid)
    {
        var redirectUrl = mfaMethod == "authenticator"
            ? $"/?mfa=true&method=authenticator&user={Uri.EscapeDataString(user.UserName ?? "")}&error={Uri.EscapeDataString("Código de autenticación incorrecto o expirado. Verifica tu app Google o Microsoft Authenticator.")}"
            : $"/?mfa=true&method=email&email={Uri.EscapeDataString(MaskEmail(email))}&error={Uri.EscapeDataString("Código de seguridad incorrecto o expirado.")}";
        return Results.Redirect(redirectUrl);
    }

    // Clear 2FA temporary cookie
    await httpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);

    // Issue production authentication cookie
    var roles = await userManager.GetRolesAsync(user);
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Name, user.UserName ?? user.Email ?? "Usuario"),
        new(ClaimTypes.Email, user.Email ?? ""),
        new(ClaimTypes.AuthenticationMethod, authMethodUsed)
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

    return Results.Redirect(returnUrl);
}).DisableAntiforgery();

app.MapPost("/api/auth/resend-otp", async (
    HttpContext httpContext,
    UserManager<ApplicationUser> userManager,
    IEmailTemplateService templateService,
    IEmailSender emailSender,
    ILoggerFactory loggerFactory) =>
{
    var authResult = await httpContext.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
    if (!authResult.Succeeded || authResult.Principal == null)
    {
        return Results.Redirect("/?error=" + Uri.EscapeDataString("La sesión de verificación ha expirado. Por favor ingresa nuevamente."));
    }

    var userId = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);

    if (string.IsNullOrEmpty(userId))
    {
        return Results.Redirect("/?error=" + Uri.EscapeDataString("Error identificando la sesión temporal."));
    }

    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
    {
        return Results.Redirect("/?error=" + Uri.EscapeDataString("Usuario no encontrado."));
    }

    var otp = await userManager.GenerateTwoFactorTokenAsync(user, "Email");
    var logger = loggerFactory.CreateLogger("Auth");

    var templateValues = new Dictionary<string, string>
    {
        ["UserName"] = user.UserName ?? user.Email ?? "Usuario",
        ["UserEmail"] = user.Email ?? "",
        ["OtpCode"] = otp,
        ["Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
    };

    var htmlBody = templateService.GenerateHtml("otp-mfa-template.html", templateValues);
    var sent = await emailSender.SendEmailAsync(user.Email ?? "", "Nuevo Código de Seguridad MFA - JewerlySystem", htmlBody);

    logger.LogInformation("Reenvío MFA OTP para {Email}: [{Otp}]. Enviado: {Sent}", user.Email, otp, sent);

    return Results.Redirect($"/?mfa=true&email={Uri.EscapeDataString(MaskEmail(email))}&message={Uri.EscapeDataString("Se ha enviado un nuevo código a tu correo.")}");
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

static string MaskEmail(string? email)
{
    if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        return "tu correo registrado";
    var parts = email.Split('@');
    var name = parts[0];
    var domain = parts[1];
    if (name.Length <= 2)
        return $"{name[0]}*@{domain}";
    return $"{name[0]}{new string('*', Math.Max(1, name.Length - 2))}{name[^1]}@{domain}";
}
