namespace JewerlySystem.Common.Application.Security;

/// <summary>
/// Provee utilitarios de sanitización y enmascaramiento de PII y secretos
/// según ISO/IEC 27701:2019 (PIMS) y OWASP Top 10:2025 A09.
/// </summary>
public static class DataSanitizer
{
    private static readonly HashSet<string> SensitiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "contrasena", "contraseña", "token", "secret", "apikey",
        "api_key", "bearer", "privatekey", "pin", "cvv", "passkey", "login",
        "credential", "otp"
    };

    /// <summary>
    /// Enmascara un correo electrónico conservando el primer carácter y el dominio.
    /// Ejemplo: usuario@dominio.com -> u***o@dominio.com
    /// </summary>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return string.Empty;
        var parts = email.Split('@');
        if (parts.Length != 2) return "[PII REDACTED]";

        var name = parts[0];
        var domain = parts[1];

        if (name.Length <= 2)
            return $"{name[0]}*@{domain}";

        return $"{name[0]}***{name[^1]}@{domain}";
    }

    /// <summary>
    /// Enmascara un número de documento o cédula mostrando solo los últimos dígitos.
    /// Ejemplo: 1020304050 -> ******4050
    /// </summary>
    public static string MaskIdentification(string? doc)
    {
        if (string.IsNullOrWhiteSpace(doc)) return string.Empty;
        if (doc.Length <= 4) return "****";
        return new string('*', doc.Length - 4) + doc[^4..];
    }

    /// <summary>
    /// Determina si un nombre de comando/consulta o propiedad contiene datos sensibles.
    /// </summary>
    public static bool IsSensitive(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return SensitiveKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Genera una representación segura para logs de auditoría o trazas de ejecución.
    /// </summary>
    public static string SanitizeForLog(object? request)
    {
        if (request is null) return "(null)";
        var typeName = request.GetType().Name;

        if (IsSensitive(typeName))
        {
            return $"[{typeName}: PAYLOAD_REDACTED_FOR_SECURITY]";
        }

        return request.ToString() ?? typeName;
    }
}
