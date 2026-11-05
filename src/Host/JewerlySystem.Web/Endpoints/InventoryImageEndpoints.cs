using JewerlySystem.Common.Application.Inventory;
using JewerlySystem.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace JewerlySystem.Web.Endpoints;

public static class InventoryImageEndpoints
{
    public static IEndpointRouteBuilder MapInventoryImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/inventory").RequireAuthorization();

        // 1. Búsqueda inteligente de fotos de relojes
        group.MapGet("/watches/search-images", async (
            [FromQuery] string brand,
            [FromQuery] string model,
            [FromQuery] string? caseNumber,
            IInventoryImageService imageService,
            CancellationToken ct) =>
        {
            var results = await imageService.SearchWatchImagesAsync(brand, model, caseNumber, ct);
            return Results.Ok(results);
        });

        // 2. Subida de imagen Base64 estándar
        group.MapPost("/upload-image", async (
            [FromBody] UploadImageRequest request,
            IInventoryImageService imageService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Base64Data))
            {
                return Results.BadRequest(new { error = "La imagen en Base64 es obligatoria." });
            }

            var relativeUrl = await imageService.SaveBase64ImageAsync(request.Base64Data, request.Prefix ?? "item", ct);
            return Results.Ok(new { url = relativeUrl });
        });

        // 3. Subida directa de foto original de celular (Respaldo inmediato garantizado en disco)
        group.MapPost("/upload-raw-photo", async (
            IFormFile file,
            [FromForm] string? prefix,
            IInventoryPhotoJobManager jobManager,
            CancellationToken ct) =>
        {
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "No se recibió ningún archivo de fotografía." });
            }

            if (file.Length > 30 * 1024 * 1024)
            {
                return Results.BadRequest(new { error = "La fotografía excede el límite máximo de 30MB." });
            }

            await using var stream = file.OpenReadStream();
            var job = await jobManager.RegisterAndProcessAsync(
                stream,
                file.FileName,
                prefix ?? "item",
                ct);

            return Results.Ok(new
            {
                success = true,
                jobId = job.JobId,
                rawUrl = job.RawUrl,
                status = job.Status.ToString()
            });
        }).DisableAntiforgery();

        // 4. Consulta de estado del trabajo en segundo plano
        group.MapGet("/photo-job/{jobId}", (
            string jobId,
            IInventoryPhotoJobManager jobManager) =>
        {
            var job = jobManager.GetJob(jobId);
            if (job == null)
            {
                return Results.NotFound(new { error = "Trabajo de procesamiento no encontrado." });
            }

            return Results.Ok(new
            {
                jobId = job.JobId,
                status = job.Status.ToString(),
                rawUrl = job.RawUrl,
                standardizedUrl = job.StandardizedUrl,
                error = job.ErrorMessage
            });
        });

        return endpoints;
    }

    public record UploadImageRequest(string Base64Data, string? Prefix = "item");
}
