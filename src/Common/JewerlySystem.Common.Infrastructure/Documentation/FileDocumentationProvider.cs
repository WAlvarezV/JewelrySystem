using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JewerlySystem.Common.Application.Documentation.Abstractions;
using Markdig;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace JewerlySystem.Common.Infrastructure.Documentation;

public sealed class FileDocumentationProvider(
    IWebHostEnvironment environment,
    ILogger<FileDocumentationProvider> logger)
    : IDocumentationProvider
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public async Task<(string Markdown, string Html)> GetContentAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        string normalizedPath = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        
        string webRoot = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            // Fallback en desarrollo o entornos de pruebas si WebRootPath no está inicializado
            string baseDir = AppContext.BaseDirectory;
            webRoot = Path.Combine(baseDir, "wwwroot");
            if (!Directory.Exists(webRoot))
            {
                // Buscar recursivamente hacia la raíz del proyecto
                string current = baseDir;
                while (!string.IsNullOrEmpty(current) && Directory.Exists(current))
                {
                    string candidate = Path.Combine(current, "src", "Host", "JewerlySystem.Web", "wwwroot");
                    if (Directory.Exists(candidate))
                    {
                        webRoot = candidate;
                        break;
                    }
                    var parent = Directory.GetParent(current);
                    if (parent == null || parent.FullName == current) break;
                    current = parent.FullName;
                }
            }
        }

        string fullPath = Path.Combine(webRoot ?? string.Empty, normalizedPath);

        if (!File.Exists(fullPath))
        {
            logger.LogWarning("Archivo físico de documentación no encontrado: {FullPath}", fullPath);
            return (string.Empty, string.Empty);
        }

        string markdownContent = await File.ReadAllTextAsync(fullPath, cancellationToken);
        string rawHtml = Markdown.ToHtml(markdownContent, Pipeline);

        // Reescribir rutas relativas de imágenes (ej. images/ejemplo.png -> /docs/modules/Inventory/images/ejemplo.png)
        string posixPath = relativePath.Replace('\\', '/');
        int lastSlashIndex = posixPath.LastIndexOf('/');
        string docDirectory = lastSlashIndex > 0 ? posixPath[..lastSlashIndex] : "docs";
        string normalizedDocDir = docDirectory.StartsWith('/') ? docDirectory : $"/{docDirectory}";
        string targetImageFolder = $"{normalizedDocDir}/images/";

        string adjustedHtml = FixImageRelativePaths(rawHtml, targetImageFolder);

        return (markdownContent, adjustedHtml);
    }

    private static string FixImageRelativePaths(string htmlContent, string targetImageFolder)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return string.Empty;
        }

        string normalizedFolder = targetImageFolder.StartsWith('/')
            ? targetImageFolder
            : $"/{targetImageFolder}";

        string targetSrc = $"src=\"{normalizedFolder}";
        string targetSrcSingle = $"src='{normalizedFolder}";

        return htmlContent
            .Replace("src=\"./images/", targetSrc, StringComparison.OrdinalIgnoreCase)
            .Replace("src=\"images/", targetSrc, StringComparison.OrdinalIgnoreCase)
            .Replace("src='./images/", targetSrcSingle, StringComparison.OrdinalIgnoreCase)
            .Replace("src='images/", targetSrcSingle, StringComparison.OrdinalIgnoreCase);
    }
}
