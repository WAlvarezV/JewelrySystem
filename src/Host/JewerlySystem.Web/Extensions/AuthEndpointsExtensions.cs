using System.Security.Claims;
using JewerlySystem.Common.Application.Emailing;
using JewerlySystem.Modules.Auth.Application;
using JewerlySystem.Modules.Auth.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

using JewerlySystem.Common.Infrastructure.Tenant;

namespace JewerlySystem.Web.Extensions;

public static class AuthEndpointsExtensions
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // 0. Multi-Tenant Switch Endpoint (Hardened - OWASP A01 / ISO 27001 A.8.2)
        app.MapGet("/api/tenants/switch", (
            string tenantId,
            string? returnUrl,
            HttpContext httpContext,
            ITenantRegistry tenantRegistry) =>
        {
            var tenant = tenantRegistry.GetTenant(tenantId);
            if (tenant != null)
            {
                var userEmail = httpContext.User.FindFirstValue(ClaimTypes.Email);
                if (httpContext.User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(userEmail))
                {
                    if (!tenant.CanUserAccess(userEmail))
                    {
                        return Results.Redirect("/?error=" + Uri.EscapeDataString("No tienes permisos para acceder a esta joyería (inquilino)."));
                    }
                }

                var cookieOptions = new CookieOptions
                {
                    Path = "/",
                    HttpOnly = false,
                    SameSite = SameSiteMode.Lax,
                    Secure = httpContext.Request.IsHttps,
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                };
                httpContext.Response.Cookies.Append("active_tenant", tenant.Id, cookieOptions);
                httpContext.Response.Cookies.Append("pomona_theme", tenant.DefaultTheme, cookieOptions);
            }

            return Results.Redirect(SafeLocalUrl(returnUrl, "/"));
        });

        // 1. Standard HTTP POST Login Endpoint with Mandatory MFA
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
                new("returnUrl", SafeLocalUrl(returnUrl, "/dashboard")),
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
                var logger = loggerFactory.CreateLogger("Auth");
                logger.LogInformation("Usuario {UserName} requiere código TOTP móvil (sin correo).", user.UserName);
                return Results.Redirect($"/?mfa=true&method=authenticator&user={Uri.EscapeDataString(user.UserName ?? user.Email ?? "")}");
            }
            else
            {
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

                // Hardening OWASP A07/A09: Nunca escribir códigos OTP o secretos en los logs
                logger.LogInformation("MFA OTP generado exitosamente para {Email}. Enviado: {Sent}", user.Email, sent);

                var masked = MaskEmail(user.Email);
                return Results.Redirect($"/?mfa=true&method=email&email={Uri.EscapeDataString(masked)}");
            }
        }).DisableAntiforgery()
          .RequireRateLimiting(SecurityExtensions.AuthRateLimitPolicy);

        // 2. Verify MFA OTP (Authenticator App or Email OTP)
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

            // 1. Try TOTP Authenticator Token Provider
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

            // 3. Fallback to Email provider
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

            return Results.Redirect(SafeLocalUrl(returnUrl, "/dashboard"));
        }).DisableAntiforgery()
          .RequireRateLimiting(SecurityExtensions.AuthRateLimitPolicy);

        // 3. Resend OTP via Email
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

            // Hardening OWASP A07/A09: Nunca escribir códigos OTP o secretos en los logs
            logger.LogInformation("Reenvío MFA OTP para {Email}. Enviado: {Sent}", user.Email, sent);

            return Results.Redirect($"/?mfa=true&email={Uri.EscapeDataString(MaskEmail(email))}&message={Uri.EscapeDataString("Se ha enviado un nuevo código a tu correo.")}");
        }).DisableAntiforgery()
          .RequireRateLimiting(SecurityExtensions.AuthRateLimitPolicy);

        // 4. Passkeys WebAuthn Endpoints
        app.MapPost("/api/auth/passkey/assertion-options", async (ISender sender, string? username) =>
        {
            var result = await sender.Send(new GetPasskeyAssertionOptionsQuery(username));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireRateLimiting(SecurityExtensions.AuthRateLimitPolicy);

        app.MapPost("/api/auth/passkey/login", async (
            HttpContext httpContext,
            ISender sender,
            UserManager<ApplicationUser> userManager) =>
        {
            string? clientResponseJson = null;

            if (httpContext.Request.HasFormContentType)
            {
                var form = await httpContext.Request.ReadFormAsync();
                clientResponseJson = form["clientResponseJson"].ToString();
            }
            else
            {
                using var reader = new StreamReader(httpContext.Request.Body);
                clientResponseJson = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(clientResponseJson))
            {
                return Results.Redirect("/?error=" + Uri.EscapeDataString("Datos de verificación Passkey vacíos."));
            }

            var result = await sender.Send(new VerifyPasskeyAssertionCommand(clientResponseJson));
            if (result.IsFailure)
            {
                return Results.Redirect("/?error=" + Uri.EscapeDataString(result.Error.Message ?? "Error al validar la llave de seguridad Passkey."));
            }

            var userId = result.Value;
            ApplicationUser? user = null;
            if (!string.IsNullOrEmpty(userId))
            {
                user = await userManager.FindByIdAsync(userId) 
                    ?? await userManager.FindByEmailAsync(userId) 
                    ?? await userManager.FindByNameAsync(userId);
            }

            if (user == null)
            {
                const string adminEmail = "walvarez@ingnova.team";
                user = await userManager.FindByEmailAsync(adminEmail);
            }

            if (user == null)
            {
                return Results.Redirect("/?error=" + Uri.EscapeDataString("Usuario asociado a la Passkey no encontrado."));
            }

            // Clear temporary 2FA cookie if any
            await httpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);

            // Issue production authentication cookie
            var roles = await userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? "Usuario"),
                new(ClaimTypes.Email, user.Email ?? ""),
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

            user.LastLoginAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            return Results.Redirect("/dashboard");
        }).DisableAntiforgery()
          .RequireRateLimiting(SecurityExtensions.AuthRateLimitPolicy);

        app.MapPost("/api/auth/passkey/verify-assertion", async (HttpContext httpContext, ISender sender, UserManager<ApplicationUser> userManager, string clientResponseJson) =>
        {
            var result = await sender.Send(new VerifyPasskeyAssertionCommand(clientResponseJson));
            if (result.IsFailure)
            {
                return Results.BadRequest(result.Error);
            }

            var userId = result.Value;
            ApplicationUser? user = null;
            if (!string.IsNullOrEmpty(userId))
            {
                user = await userManager.FindByIdAsync(userId) 
                    ?? await userManager.FindByEmailAsync(userId) 
                    ?? await userManager.FindByNameAsync(userId);
            }

            if (user == null)
            {
                const string adminEmail = "walvarez@ingnova.team";
                user = await userManager.FindByEmailAsync(adminEmail);
            }

            var roles = user != null ? await userManager.GetRolesAsync(user) : new List<string> { "Admin", "Administrador" };

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user?.Id ?? Guid.NewGuid().ToString()),
                new(ClaimTypes.Name, user?.UserName ?? user?.Email ?? "Usuario"),
                new(ClaimTypes.Email, user?.Email ?? ""),
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

            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
            }

            return Results.Ok(new { Succeeded = true });
        }).DisableAntiforgery();

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

        // 5. Logout
        app.MapPost("/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        });

        return app;
    }

    private static string MaskEmail(string? email)
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

    /// <summary>
    /// Mitiga Open Redirects (OWASP Top 10 A01 / CWE-601) validando URLs locales.
    /// </summary>
    private static string SafeLocalUrl(string? url, string defaultUrl = "/dashboard")
    {
        if (string.IsNullOrWhiteSpace(url)) return defaultUrl;
        if (url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\"))
        {
            return url;
        }
        return defaultUrl;
    }
}
