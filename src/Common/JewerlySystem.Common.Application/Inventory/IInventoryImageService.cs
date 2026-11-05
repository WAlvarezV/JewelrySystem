namespace JewerlySystem.Common.Application.Inventory;

public record ImageSearchResult(
    string Title,
    string ImageUrl,
    string ThumbnailUrl,
    string Source);

public interface IInventoryImageService
{
    Task<List<ImageSearchResult>> SearchWatchImagesAsync(string brand, string model, string? caseNumber = null, CancellationToken ct = default);
    Task<string> SaveBase64ImageAsync(string base64Data, string prefix = "item", CancellationToken ct = default);
    bool DeleteImage(string? imageUrl);
}
