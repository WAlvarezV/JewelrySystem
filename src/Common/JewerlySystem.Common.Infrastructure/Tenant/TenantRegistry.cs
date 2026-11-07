using JewerlySystem.Common.Application;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace JewerlySystem.Common.Infrastructure.Tenant;

public interface ITenantRegistry
{
    IReadOnlyList<TenantInfo> GetAllTenants();
    TenantInfo? GetTenant(string tenantId);
    TenantInfo GetDefaultTenant();
    string GetConnectionString(string tenantId);
}

public class TenantRegistry : ITenantRegistry
{
    private readonly List<TenantInfo> _tenants = new();
    private readonly string _defaultTenantId = "pomona";

    public TenantRegistry(IConfiguration configuration)
    {
        // 1. Resolve raw base connection string from config or Railway environment
        var rawDefaultCs = configuration.GetConnectionString("PomonaCs");
        if (string.IsNullOrWhiteSpace(rawDefaultCs))
            rawDefaultCs = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(rawDefaultCs))
            rawDefaultCs = configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(rawDefaultCs))
            rawDefaultCs = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(rawDefaultCs))
            rawDefaultCs = "Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN";

        var defaultCs = NormalizeNpgsqlConnectionString(rawDefaultCs);

        var rawOroPlataCs = configuration.GetConnectionString("OroPlataCs");
        string oroPlataCs;

        if (!string.IsNullOrWhiteSpace(rawOroPlataCs))
        {
            oroPlataCs = NormalizeNpgsqlConnectionString(rawOroPlataCs);
        }
        else
        {
            oroPlataCs = DeriveDatabaseConnectionString(defaultCs, "_OroPlata");
        }

        // 2. Register Tenant: Joyería Pomona
        _tenants.Add(new TenantInfo(
            Id: "pomona",
            Name: "Joyería Pomona",
            ShortName: "POMONA",
            ConnectionString: defaultCs,
            DefaultTheme: "pomona-imperial",
            SymbolUrl: "images/pomona-symbol-white.png",
            LogoUrl: "images/pomona-logo-square.png",
            AllowedUserEmails: new List<string> { "walvarez@ingnova.team", "walvarez@bezion.tech", "admin@joyeriapomona.com", "william.alvarezv@gmail.com", "joglisjjc@gmail.com" }
        ));

        // 3. Register Tenant: Joyería Oro & Plata
        _tenants.Add(new TenantInfo(
            Id: "oro-plata",
            Name: "Joyería Oro & Plata",
            ShortName: "ORO & PLATA",
            ConnectionString: oroPlataCs,
            DefaultTheme: "oroplata-classic",
            SymbolUrl: "images/tenants/oro-plata/oroplata-symbol.svg",
            LogoUrl: "images/tenants/oro-plata/oroplata-logo.png",
            AllowedUserEmails: new List<string> { "walvarez@ingnova.team", "walvarez@bezion.tech" }
        ));

        // 4. Register Tenant: Joyería BeZion (Test)
        var rawBeZionCs = configuration.GetConnectionString("BeZionCs");
        string bezionCs;
        if (!string.IsNullOrWhiteSpace(rawBeZionCs))
        {
            bezionCs = NormalizeNpgsqlConnectionString(rawBeZionCs);
        }
        else
        {
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(defaultCs)
                {
                    Database = "Jewerly_BeZion"
                };
                bezionCs = builder.ConnectionString;
            }
            catch
            {
                bezionCs = defaultCs.Replace("Database=IntZaioNJewerly", "Database=Jewerly_BeZion");
            }
        }

        _tenants.Add(new TenantInfo(
            Id: "bezion",
            Name: "Joyería BeZion",
            ShortName: "BEZION",
            ConnectionString: bezionCs,
            DefaultTheme: "pomona-sapphire",
            SymbolUrl: "images/tenants/oro-plata/oroplata-symbol.svg",
            LogoUrl: "images/tenants/oro-plata/oroplata-logo.png",
            AllowedUserEmails: new List<string> { "walvarez@bezion.tech", "jmalvarezc@bezion.tech", "walvarez@ingnova.team" }
        ));
    }

    public IReadOnlyList<TenantInfo> GetAllTenants() => _tenants;

    public TenantInfo? GetTenant(string tenantId) =>
        _tenants.FirstOrDefault(t => string.Equals(t.Id, tenantId, StringComparison.OrdinalIgnoreCase));

    public TenantInfo GetDefaultTenant() =>
        GetTenant(_defaultTenantId) ?? _tenants.First();

    public string GetConnectionString(string tenantId)
    {
        var tenant = GetTenant(tenantId);
        return tenant?.ConnectionString ?? GetDefaultTenant().ConnectionString;
    }

    public static string NormalizeNpgsqlConnectionString(string cs)
    {
        if (string.IsNullOrWhiteSpace(cs)) return cs;

        if (cs.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            cs.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(cs);
                var userInfo = uri.UserInfo.Split(':');
                var user = Uri.UnescapeDataString(userInfo[0]);
                var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var db = uri.AbsolutePath.TrimStart('/');

                return $"Host={host};Port={port};Database={db};Username={user};Password={pass};Trust Server Certificate=true;Include Error Detail=true;";
            }
            catch
            {
                // Fallback to original
            }
        }

        return cs;
    }

    private static string DeriveDatabaseConnectionString(string normalizedBaseCs, string suffix)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(normalizedBaseCs);
            var originalDb = builder.Database ?? "IntZaioNJewerly";
            builder.Database = originalDb.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? originalDb
                : $"{originalDb}{suffix}";
            return builder.ConnectionString;
        }
        catch
        {
            return normalizedBaseCs.Replace("Database=IntZaioNJewerly", $"Database=IntZaioNJewerly{suffix}");
        }
    }
}
