using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

namespace JewerlySystem.Web.Extensions;

/// <summary>
/// Hardening integral de seguridad web conforme a:
/// - ISO/IEC 27001:2022 (A.8.20, A.8.24)
/// - SOC 2 Type II (CC6.6, CC6.7, A1.1)
/// - ISO/IEC 27701:2019 (Cl. 7.2.5, Cl. 7.4.4)
/// - OWASP Top 10:2025 (A01, A02, A06, A07, A10)
/// </summary>
public static class SecurityExtensions
{
    public const string AuthRateLimitPolicy = "auth-policy";
    public const string GeneralRateLimitPolicy = "general-policy";

    public static IServiceCollection AddSecurityHardening(this IServiceCollection services)
    {
        // 1. Rate Limiting (OWASP A06 Insecure Design / SOC 2 Availability A1.1 / OWASP A07)
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Auth Policy: Máx 10 peticiones por minuto por IP para prevenir ataques de fuerza bruta
            options.AddPolicy(AuthRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown_client",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // General Policy: Máx 150 peticiones por minuto por IP
            options.AddPolicy(GeneralRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown_client",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 150,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));
        });

        // 2. ProblemDetails estandarizado sin filtrar Stack Traces (OWASP A10 / ISO 27001 A.8.20)
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
            };
        });

        return services;
    }

    /// <summary>
    /// Middleware de inyección de Cabeceras de Seguridad HTTP estrictas.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;

            // Anti-Clickjacking (OWASP A02 / SOC 2 CC6.6)
            headers["X-Frame-Options"] = "DENY";

            // MIME-Sniffing Defense
            headers["X-Content-Type-Options"] = "nosniff";

            // Control estricto de Referrer para prevenir fuga de metadatos/PII (ISO 27701)
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Menor privilegio sobre APIs de hardware y navegador
            headers["Permissions-Policy"] = "camera=(), microphone=(self), geolocation=(), payment=()";

            // Bloqueo de políticas cross-domain no deseadas
            headers["X-Permitted-Cross-Domain-Policies"] = "none";

            // Deshabilitar filtro XSS obsoleto que generaba bypasses; CSP es la defensa oficial
            headers["X-XSS-Protection"] = "0";

            // Ocultar cabeceras que divulguen software base
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            // Content Security Policy optimizado para Blazor Server (WebSockets wss/ws y DOM morphing)
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
                "img-src 'self' data: https:; " +
                "font-src 'self' data: https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
                "connect-src 'self' ws: wss: http: https:; " +
                "frame-ancestors 'none'; " +
                "object-src 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';";

            await next();
        });
    }
}
