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
        // 1. Resolve base connection strings
        var defaultCs = configuration.GetConnectionString("PomonaCs")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=IntZaioNJewerly;Username=postgres;Password=IntZaioN";

        var oroPlataCs = configuration.GetConnectionString("OroPlataCs");

        // Derive OroPlata database name from defaultCs if not explicitly set
        if (string.IsNullOrWhiteSpace(oroPlataCs))
        {
            try
            {
                var npgsqlBuilder = new NpgsqlConnectionStringBuilder(defaultCs);
                var originalDb = npgsqlBuilder.Database ?? "IntZaioNJewerly";
                npgsqlBuilder.Database = originalDb.EndsWith("_OroPlata", StringComparison.OrdinalIgnoreCase)
                    ? originalDb
                    : $"{originalDb}_OroPlata";
                oroPlataCs = npgsqlBuilder.ConnectionString;
            }
            catch
            {
                oroPlataCs = defaultCs.Replace("Database=IntZaioNJewerly", "Database=IntZaioNJewerly_OroPlata");
            }
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
            AllowedUserEmails: new List<string> { "walvarez@ingnova.team", "admin@joyeriapomona.com", "william.alvarezv@gmail.com" }
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
            AllowedUserEmails: new List<string> { "walvarez@ingnova.team" }
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
}
