using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace JewerlySystem.Web.Services;

public enum PhotoJobStatus
{
    SavedRaw,
    Processing,
    Completed,
    Failed
}

public class PhotoProcessingJob
{
    public string JobId { get; init; } = string.Empty;
    public string RawUrl { get; set; } = string.Empty;
    public string? StandardizedUrl { get; set; }
    public PhotoJobStatus Status { get; set; } = PhotoJobStatus.SavedRaw;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public interface IInventoryPhotoJobManager
{
    Task<PhotoProcessingJob> RegisterAndProcessAsync(
        Stream rawStream,
        string originalFileName,
        string prefix = "item",
        CancellationToken ct = default);

    PhotoProcessingJob? GetJob(string jobId);
}

public class InventoryPhotoJobManager : IInventoryPhotoJobManager
{
    private readonly ConcurrentDictionary<string, PhotoProcessingJob> _jobs = new();
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<InventoryPhotoJobManager> _logger;

    public InventoryPhotoJobManager(
        IWebHostEnvironment env,
        ILogger<InventoryPhotoJobManager> logger)
    {
        _env = env;
        _logger = logger;
    }

    public PhotoProcessingJob? GetJob(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId)) return null;
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    public async Task<PhotoProcessingJob> RegisterAndProcessAsync(
        Stream rawStream,
        string originalFileName,
        string prefix = "item",
        CancellationToken ct = default)
    {
        var jobId = Guid.NewGuid().ToString("N")[..12];
        var safePrefix = Regex.Replace(prefix ?? "item", "[^a-zA-Z0-9_-]", "");
        if (string.IsNullOrEmpty(safePrefix)) safePrefix = "item";

        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 5) ext = ".jpg";

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var rawDir = Path.Combine(webRoot, "uploads", "inventory", "raw");
        var catalogDir = Path.Combine(webRoot, "uploads", "inventory", "catalog");
        Directory.CreateDirectory(rawDir);
        Directory.CreateDirectory(catalogDir);

        var rawFileName = $"{safePrefix}_raw_{jobId}{ext}";
        var rawFullPath = Path.Combine(rawDir, rawFileName);

        // PASO 1 (CRÍTICO): Guardar la foto original de inmediato en disco para que NUNCA se pierda
        await using (var fileStream = File.Create(rawFullPath))
        {
            await rawStream.CopyToAsync(fileStream, ct);
        }

        var rawUrl = $"/uploads/inventory/raw/{rawFileName}";
        _logger.LogInformation("Fotografía original de celular asegurada en servidor: {RawUrl} (Job: {JobId})", rawUrl, jobId);

        var job = new PhotoProcessingJob
        {
            JobId = jobId,
            RawUrl = rawUrl,
            Status = PhotoJobStatus.Processing,
            CreatedAt = DateTime.UtcNow
        };

        _jobs[jobId] = job;

        // PASO 2: Lanzar la estandarización y sellado en SEGUNDO PLANO
        _ = Task.Run(() => ProcessImageInBackground(job, rawFullPath, catalogDir, safePrefix, jobId));

        return job;
    }

    private void ProcessImageInBackground(
        PhotoProcessingJob job,
        string rawFullPath,
        string catalogDir,
        string safePrefix,
        string jobId)
    {
        try
        {
            _logger.LogInformation("Iniciando estandarización en segundo plano para Job {JobId}...", jobId);

            // 1. Decodificar y respetar orientación EXIF de celulares
            using var inputStream = File.OpenRead(rawFullPath);
            using var codec = SKCodec.Create(inputStream);
            var origin = codec?.EncodedOrigin ?? SKEncodedOrigin.TopLeft;

            inputStream.Position = 0;
            using var rawBitmap = SKBitmap.Decode(inputStream);

            if (rawBitmap == null)
            {
                throw new InvalidOperationException("No se pudo decodificar el mapa de bits de la imagen.");
            }

            using var orientedBitmap = AutoOrient(rawBitmap, origin);

            // 2. Definir lienzo cuadrado estandarizado (900x900)
            const int targetDimension = 900;
            int origW = orientedBitmap.Width;
            int origH = orientedBitmap.Height;

            int scaledW = origW;
            int scaledH = origH;

            if (origW > targetDimension || origH > targetDimension)
            {
                if (origW > origH)
                {
                    scaledW = targetDimension;
                    scaledH = (int)Math.Round((double)origH * targetDimension / origW);
                }
                else
                {
                    scaledH = targetDimension;
                    scaledW = (int)Math.Round((double)origW * targetDimension / origH);
                }
            }

            int squareSize = Math.Max(scaledW, scaledH);
            int margin = (int)Math.Round(squareSize * 0.05); // Margen de vitrina 5%
            int finalCanvasSize = squareSize + (margin * 2);

            using var standardizedBitmap = new SKBitmap(finalCanvasSize, finalCanvasSize);
            using (var canvas = new SKCanvas(standardizedBitmap))
            {
                // Fondo blanco puro #FFFFFF
                canvas.Clear(SKColors.White);

                // Dibujar imagen centrada
                int offsetX = margin + (squareSize - scaledW) / 2;
                int offsetY = margin + (squareSize - scaledH) / 2;

                var destRect = new SKRect(offsetX, offsetY, offsetX + scaledW, offsetY + scaledH);
                canvas.DrawBitmap(orientedBitmap, destRect, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            }

            // 3. Estandarización de fondo blanco (eliminar ruido de vitrina / mesa clara)
            var pixels = standardizedBitmap.Pixels;
            for (int i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                int r = c.Red;
                int g = c.Green;
                int b = c.Blue;
                int max = Math.Max(r, Math.Max(g, b));
                int min = Math.Min(r, Math.Min(g, b));
                int delta = max - min;

                // Si es un pixel claro y con poca saturación, forzar a blanco puro #FFFFFF
                if (r > 210 && g > 210 && b > 210 && delta < 25)
                {
                    pixels[i] = SKColors.White;
                }
                else if (r > 195 && g > 195 && b > 195 && delta < 15)
                {
                    pixels[i] = SKColors.White;
                }
            }
            standardizedBitmap.Pixels = pixels;

            // 4. Marca de agua genérica de alta gama (universal e interoperable)
            using (var canvas = new SKCanvas(standardizedBitmap))
            {
                float finalSize = finalCanvasSize;
                float barHeight = Math.Max(42f, finalSize * 0.065f);
                float barY = finalSize - barHeight;

                // Barra inferior blanca translúcida para legibilidad
                using var barPaint = new SKPaint
                {
                    Color = new SKColor(255, 255, 255, 225),
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRect(new SKRect(0, barY, finalSize, finalSize), barPaint);

                // Línea dorada superior de lujo (#D4AF37)
                using var linePaint = new SKPaint
                {
                    Color = new SKColor(212, 175, 55, 210),
                    StrokeWidth = 2f,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke
                };
                canvas.DrawLine(finalSize * 0.08f, barY, finalSize * 0.92f, barY, linePaint);

                // Texto de autenticidad genérico
                float fontSize = Math.Max(13f, finalSize * 0.024f);
                using var font = new SKFont(SKTypeface.Default, fontSize);
                const string watermarkText = "✦  FINE JEWELRY & LUXURY TIMEPIECES • PIEZA VERIFICADA  ✦";
                float textY = barY + (barHeight / 2f) + (fontSize / 3f);

                using var textPaint = new SKPaint
                {
                    Color = new SKColor(140, 115, 53), // Dorado noble #8C7335
                    IsAntialias = true
                };

                canvas.DrawText(watermarkText, finalSize / 2f, textY, SKTextAlign.Center, font, textPaint);
            }

            // 5. Exportar a WebP de alta fidelidad y bajo peso (< 80 KB)
            var finalFileName = $"{safePrefix}_{jobId}.webp";
            var finalFullPath = Path.Combine(catalogDir, finalFileName);

            using var image = SKImage.FromBitmap(standardizedBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Webp, 85) 
                          ?? image.Encode(SKEncodedImageFormat.Jpeg, 90);

            using (var outFs = File.Create(finalFullPath))
            {
                data.SaveTo(outFs);
            }

            job.StandardizedUrl = $"/uploads/inventory/catalog/{finalFileName}";
            job.Status = PhotoJobStatus.Completed;

            _logger.LogInformation("Job {JobId} completado con éxito: {FinalUrl}", jobId, job.StandardizedUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando estandarización de foto para Job {JobId}. Se usará la foto original garantizada.", jobId);
            
            // Garantía: si falla el procesamiento gráfico, NUNCA se pierde la foto, se usa la original
            job.StandardizedUrl = job.RawUrl;
            job.Status = PhotoJobStatus.Completed;
            job.ErrorMessage = ex.Message;
        }
    }

    private static SKBitmap AutoOrient(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        return origin switch
        {
            SKEncodedOrigin.RightTop => RotateBitmap(bitmap, 90),
            SKEncodedOrigin.BottomRight => RotateBitmap(bitmap, 180),
            SKEncodedOrigin.LeftBottom => RotateBitmap(bitmap, 270),
            _ => bitmap
        };
    }

    private static SKBitmap RotateBitmap(SKBitmap bitmap, float degrees)
    {
        bool is90Or270 = degrees == 90 || degrees == 270;
        int newWidth = is90Or270 ? bitmap.Height : bitmap.Width;
        int newHeight = is90Or270 ? bitmap.Width : bitmap.Height;

        var rotated = new SKBitmap(newWidth, newHeight);
        using var canvas = new SKCanvas(rotated);
        canvas.Clear(SKColors.Transparent);
        canvas.Translate(newWidth / 2f, newHeight / 2f);
        canvas.RotateDegrees(degrees);
        canvas.Translate(-bitmap.Width / 2f, -bitmap.Height / 2f);
        canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
        return rotated;
    }
}
