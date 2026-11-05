using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using JewerlySystem.Common.Presentation.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace JewerlySystem.Web.Services;

/// <summary>
/// Registro dinámico de mapeo de rutas Blazor a secciones del manual de usuario.
/// Lee la configuración en tiempo de ejecución desde wwwroot/docs/help-routes.json sin requerir recompilar.
/// </summary>
public sealed class HelpRouteRegistry(
    IWebHostEnvironment environment,
    ILogger<HelpRouteRegistry> logger)
    : IHelpRouteRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private Dictionary<string, HelpRouteInfo>? _cachedMappings;
    private DateTime _lastReadTime = DateTime.MinValue;
    private readonly object _lock = new();

    public HelpRouteInfo GetHelpInfoForRoute(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return new HelpRouteInfo("Inventario", "docs/modules/Inventory/ManualUsuario.md", CustomTitle: "Manual de Usuario del Sistema");
        }

        string cleanPath = relativePath.Split('?')[0].Trim();
        if (!cleanPath.StartsWith('/'))
        {
            cleanPath = "/" + cleanPath;
        }

        Dictionary<string, HelpRouteInfo> mappings = GetOrLoadMappings();

        if (mappings.TryGetValue(cleanPath, out HelpRouteInfo? match))
        {
            return match;
        }

        // Búsqueda por prefijo si la ruta contiene sub-rutas o IDs
        foreach (var (routeKey, info) in mappings)
        {
            if (cleanPath.StartsWith(routeKey, StringComparison.OrdinalIgnoreCase) && routeKey != "/")
            {
                return info;
            }
        }

        // Fallbacks inteligentes basados en el segmento de ruta
        string[] segments = cleanPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string firstSegment = segments.Length > 0 ? segments[0].ToLowerInvariant() : "";

        if (firstSegment is "inventory" or "jewelry" or "watches" or "articulos")
        {
            return new HelpRouteInfo("Inventario", "docs/modules/Inventory/ManualUsuario.md", "proceso-1-catalogo", "Ayuda — Inventario y Catálogo");
        }
        if (firstSegment is "contracts")
        {
            return new HelpRouteInfo("Contratos", "docs/modules/Inventory/ManualUsuario.md", "proceso-3-creacion-relojes", "Ayuda — Contratos y Empeños");
        }
        if (firstSegment is "cash")
        {
            return new HelpRouteInfo("Caja", "docs/modules/Inventory/ManualUsuario.md", "proceso-5-descargo", "Ayuda — Arqueo y Caja");
        }
        if (firstSegment is "customers")
        {
            return new HelpRouteInfo("Clientes", "docs/modules/Inventory/ManualUsuario.md", null, "Ayuda — Directorio de Clientes");
        }
        if (firstSegment is "invoicing")
        {
            return new HelpRouteInfo("Facturación", "docs/modules/Inventory/ManualUsuario.md", "proceso-5-descargo", "Ayuda — Facturación y Ventas");
        }

        return new HelpRouteInfo("Sistema Pomona", "docs/modules/Inventory/ManualUsuario.md", null, "Manual de Usuario del Sistema");
    }

    private Dictionary<string, HelpRouteInfo> GetOrLoadMappings()
    {
        lock (_lock)
        {
            try
            {
                string jsonPath = Path.Combine(environment.WebRootPath ?? string.Empty, "docs", "help-routes.json");
                if (!File.Exists(jsonPath))
                {
                    return FallbackMappings();
                }

                DateTime lastWrite = File.GetLastWriteTimeUtc(jsonPath);
                if (_cachedMappings != null && lastWrite <= _lastReadTime)
                {
                    return _cachedMappings;
                }

                string jsonContent = File.ReadAllText(jsonPath);
                var data = JsonSerializer.Deserialize<Dictionary<string, HelpRouteInfo>>(jsonContent, JsonOptions);

                var result = new Dictionary<string, HelpRouteInfo>(StringComparer.OrdinalIgnoreCase);
                if (data != null)
                {
                    foreach (var (key, val) in data)
                    {
                        result[key] = val;
                    }
                }

                _cachedMappings = result;
                _lastReadTime = lastWrite;
                return result;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al leer help-routes.json. Usando mapeos por defecto.");
                return FallbackMappings();
            }
        }
    }

    private static Dictionary<string, HelpRouteInfo> FallbackMappings()
    {
        return new Dictionary<string, HelpRouteInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["/inventory"] = new("Inventario", "docs/modules/Inventory/ManualUsuario.md", "proceso-1-catalogo", "Ayuda — Catálogo de Inventario"),
            ["/jewelry"] = new("Inventario", "docs/modules/Inventory/ManualUsuario.md", "proceso-1-catalogo", "Ayuda — Joyas"),
            ["/watches"] = new("Inventario", "docs/modules/Inventory/ManualUsuario.md", "proceso-1-catalogo", "Ayuda — Relojes"),
            ["/inventory/discharge"] = new("Inventario", "docs/modules/Inventory/ManualUsuario.md", "proceso-5-descargo", "Ayuda — Descargo y Venta"),
            ["/inventory/consolidated"] = new("Inventario", "docs/modules/Inventory/ManualUsuario.md", "proceso-1-catalogo", "Ayuda — Consolidado de Inventario"),
            ["/contracts"] = new("Contratos", "docs/modules/Inventory/ManualUsuario.md", "proceso-3-creacion-relojes", "Ayuda — Contratos de Joyería"),
            ["/contracts/create"] = new("Contratos", "docs/modules/Inventory/ManualUsuario.md", null, "Ayuda — Nuevo Contrato"),
            ["/customers"] = new("Clientes", "docs/modules/Inventory/ManualUsuario.md", null, "Ayuda — Gestión de Clientes"),
            ["/invoicing"] = new("Facturación", "docs/modules/Inventory/ManualUsuario.md", "proceso-5-descargo", "Ayuda — Facturación"),
            ["/cash/daily"] = new("Caja", "docs/modules/Inventory/ManualUsuario.md", null, "Ayuda — Cuadre Diario de Caja"),
            ["/cash/consolidated"] = new("Caja", "docs/modules/Inventory/ManualUsuario.md", null, "Ayuda — Consolidado de Caja")
        };
    }
}
