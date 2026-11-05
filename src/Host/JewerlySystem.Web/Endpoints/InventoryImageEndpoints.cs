using JewerlySystem.Common.Application.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace JewerlySystem.Web.Endpoints;

public static class InventoryImageEndpoints
{
    public static IEndpointRouteBuilder MapInventoryImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/inventory").RequireAuthorization();

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

        return endpoints;
    }

    public record UploadImageRequest(string Base64Data, string? Prefix = "item");
}
