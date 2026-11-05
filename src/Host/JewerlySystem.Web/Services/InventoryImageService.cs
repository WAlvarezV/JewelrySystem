using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using JewerlySystem.Common.Application.Inventory;

namespace JewerlySystem.Web.Services;

public partial class InventoryImageService : IInventoryImageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<InventoryImageService> _logger;

    public InventoryImageService(
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment env,
        ILogger<InventoryImageService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _env = env;
        _logger = logger;
    }

    public async Task<List<ImageSearchResult>> SearchWatchImagesAsync(
        string brand,
        string model,
        string? caseNumber = null,
        CancellationToken ct = default)
    {
        var results = new List<ImageSearchResult>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(brand)) queryParts.Add(brand.Trim());
        if (!string.IsNullOrWhiteSpace(model)) queryParts.Add(model.Trim());
        var query = string.Join(" ", queryParts);

        if (string.IsNullOrWhiteSpace(query))
        {
            return results;
        }

        // 1. Wikimedia Commons API
        try
        {
            var wikimediaResults = await QueryWikimediaCommonsAsync(query, ct);
            foreach (var r in wikimediaResults)
            {
                if (seenUrls.Add(r.ImageUrl))
                {
                    results.Add(r);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Wikimedia search failed for query '{Query}'", query);
        }

        // 2. Wikipedia PageImages (si hay pocos resultados)
        if (results.Count < 6)
        {
            try
            {
                var wikiResults = await QueryWikipediaPagesAsync(query, ct);
                foreach (var r in wikiResults)
                {
                    if (seenUrls.Add(r.ImageUrl))
                    {
                        results.Add(r);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Wikipedia search failed for query '{Query}'", query);
            }
        }

        // 3. DuckDuckGo Image Search (si aún hay pocos resultados)
        if (results.Count < 6)
        {
            try
            {
                var ddgResults = await QueryDuckDuckGoAsync(query + " watch", ct);
                foreach (var r in ddgResults)
                {
                    if (seenUrls.Add(r.ImageUrl))
                    {
                        results.Add(r);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DuckDuckGo search failed for query '{Query}'", query);
            }
        }

        return results.Take(20).ToList();
    }

    private async Task<List<ImageSearchResult>> QueryWikimediaCommonsAsync(string query, CancellationToken ct)
    {
        var list = new List<ImageSearchResult>();
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("JewelrySystemCatalog", "1.0"));

        var url = $"https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrsearch={Uri.EscapeDataString(query)}&gsrnamespace=6&gsrlimit=12&prop=imageinfo&iiprop=url|mime&iiurlwidth=500&format=json";

        var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return list;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("query", out var queryElem) &&
            queryElem.TryGetProperty("pages", out var pagesElem))
        {
            foreach (var page in pagesElem.EnumerateObject())
            {
                var title = page.Value.TryGetProperty("title", out var titleProp)
                    ? titleProp.GetString()?.Replace("File:", "") ?? ""
                    : "";

                if (page.Value.TryGetProperty("imageinfo", out var infoArr) && infoArr.GetArrayLength() > 0)
                {
                    var firstInfo = infoArr[0];
                    var thumbUrl = firstInfo.TryGetProperty("thumburl", out var thumbProp) ? thumbProp.GetString() : null;
                    var origUrl = firstInfo.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;

                    var chosenThumb = !string.IsNullOrEmpty(thumbUrl) ? thumbUrl : origUrl;
                    var chosenFull = !string.IsNullOrEmpty(origUrl) ? origUrl : thumbUrl;

                    if (!string.IsNullOrEmpty(chosenThumb))
                    {
                        list.Add(new ImageSearchResult(
                            Title: CleanTitle(title),
                            ImageUrl: chosenFull!,
                            ThumbnailUrl: chosenThumb,
                            Source: "Wikimedia Commons"
                        ));
                    }
                }
            }
        }

        return list;
    }

    private async Task<List<ImageSearchResult>> QueryWikipediaPagesAsync(string query, CancellationToken ct)
    {
        var list = new List<ImageSearchResult>();
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("JewelrySystemCatalog", "1.0"));

        var url = $"https://en.wikipedia.org/w/api.php?action=query&generator=search&gsrsearch={Uri.EscapeDataString(query)}&gsrlimit=8&prop=pageimages&pithumbsize=500&format=json";

        var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return list;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("query", out var queryElem) &&
            queryElem.TryGetProperty("pages", out var pagesElem))
        {
            foreach (var page in pagesElem.EnumerateObject())
            {
                var title = page.Value.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
                if (page.Value.TryGetProperty("thumbnail", out var thumbElem))
                {
                    var source = thumbElem.TryGetProperty("source", out var srcProp) ? srcProp.GetString() : null;
                    if (!string.IsNullOrEmpty(source))
                    {
                        list.Add(new ImageSearchResult(
                            Title: CleanTitle(title),
                            ImageUrl: source,
                            ThumbnailUrl: source,
                            Source: "Wikipedia"
                        ));
                    }
                }
            }
        }

        return list;
    }

    private async Task<List<ImageSearchResult>> QueryDuckDuckGoAsync(string query, CancellationToken ct)
    {
        var list = new List<ImageSearchResult>();
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        // 1. Obtener token VQD
        var pageUrl = $"https://duckduckgo.com/?q={Uri.EscapeDataString(query)}";
        var pageRes = await client.GetStringAsync(pageUrl, ct);

        var match = VqdRegex().Match(pageRes);
        if (!match.Success) return list;

        var vqd = match.Groups[1].Value;

        // 2. Consultar API de imágenes
        var apiUrl = $"https://duckduckgo.com/i.js?l=wt-wt&o=json&q={Uri.EscapeDataString(query)}&vqd={vqd}&p=1";
        var apiRes = await client.GetAsync(apiUrl, ct);
        if (!apiRes.IsSuccessStatusCode) return list;

        var json = await apiRes.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("results", out var resultsElem))
        {
            foreach (var item in resultsElem.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var tp) ? tp.GetString() ?? "" : "";
                var img = item.TryGetProperty("image", out var ip) ? ip.GetString() : null;
                var thumb = item.TryGetProperty("thumbnail", out var thp) ? thp.GetString() : null;

                if (!string.IsNullOrEmpty(img))
                {
                    list.Add(new ImageSearchResult(
                        Title: CleanTitle(title),
                        ImageUrl: img,
                        ThumbnailUrl: thumb ?? img,
                        Source: "Web Catalog"
                    ));
                }

                if (list.Count >= 10) break;
            }
        }

        return list;
    }

    public async Task<string> SaveBase64ImageAsync(string base64Data, string prefix = "item", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(base64Data))
        {
            throw new ArgumentException("Los datos de la imagen en base64 no pueden estar vacíos.", nameof(base64Data));
        }

        var extension = ".webp";
        var cleanBase64 = base64Data;

        // Detectar y remover prefijo data:image/...;base64,
        var commaIdx = base64Data.IndexOf(',');
        if (commaIdx >= 0)
        {
            var header = base64Data[..commaIdx].ToLowerInvariant();
            if (header.Contains("image/png")) extension = ".png";
            else if (header.Contains("image/jpeg") || header.Contains("image/jpg")) extension = ".jpg";
            else if (header.Contains("image/webp")) extension = ".webp";

            cleanBase64 = base64Data[(commaIdx + 1)..];
        }

        var bytes = Convert.FromBase64String(cleanBase64);

        if (bytes.Length > 15 * 1024 * 1024)
        {
            throw new InvalidOperationException("La imagen excede el límite permitido de 15MB.");
        }

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var targetFolder = Path.Combine(webRoot, "uploads", "inventory", "catalog");
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        var safePrefix = Regex.Replace(prefix, "[^a-zA-Z0-9_-]", "");
        if (string.IsNullOrEmpty(safePrefix)) safePrefix = "item";

        var fileName = $"{safePrefix}_{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetFolder, fileName);

        await File.WriteAllBytesAsync(fullPath, bytes, ct);

        return $"/uploads/inventory/catalog/{fileName}";
    }

    public bool DeleteImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return false;

        // Validar que solo borre archivos dentro de /uploads/inventory/catalog/ para prevenir path traversal
        if (!imageUrl.StartsWith("/uploads/inventory/catalog/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var fileName = Path.GetFileName(imageUrl);
            var fullPath = Path.Combine(webRoot, "uploads", "inventory", "catalog", fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo eliminar el archivo físico de la imagen {ImageUrl}", imageUrl);
        }

        return false;
    }

    private static string CleanTitle(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Pieza de Relojería";
        var cleaned = Regex.Replace(raw, @"\.[a-zA-Z0-9]{3,4}$", "");
        cleaned = cleaned.Replace("_", " ").Trim();
        return cleaned.Length > 80 ? cleaned[..80] + "..." : cleaned;
    }

    [GeneratedRegex(@"vqd=""([^""]+)""")]
    private static partial Regex VqdRegex();
}
