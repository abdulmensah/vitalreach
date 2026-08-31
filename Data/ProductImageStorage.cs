using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Data.Sqlite;

namespace VitalReach.Web.Data;

public sealed class ProductImageStorage(IConfiguration configuration, IWebHostEnvironment environment)
{
    public const long MaxFileSize = 5 * 1024 * 1024;
    private const string RequestPath = "/uploads/products";

    private readonly string _storagePath = ResolveStoragePath(configuration, environment);

    public static string ResolveStoragePath(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configured = configuration["ProductImages:Path"];
        if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured);

        var connectionString = configuration.GetConnectionString("Catalog");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var databasePath = new SqliteConnectionStringBuilder(connectionString).DataSource;
            if (Path.IsPathRooted(databasePath))
                return Path.Combine(Path.GetDirectoryName(databasePath)!, "uploads", "products");
        }

        return Path.Combine(environment.ContentRootPath, "App_Data", "uploads", "products");
    }

    public async Task<string> SaveAsync(IBrowserFile image, CancellationToken cancellationToken = default)
    {
        var extension = GetValidatedExtension(image);
        Directory.CreateDirectory(_storagePath);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var destination = Path.Combine(_storagePath, fileName);
        await using var input = image.OpenReadStream(MaxFileSize, cancellationToken);
        await using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
        {
            await input.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        if (!HasExpectedSignature(destination, extension))
        {
            File.Delete(destination);
            throw new InvalidOperationException("The selected file does not contain a valid JPG, PNG, or WebP image.");
        }

        return $"{RequestPath}/{fileName}";
    }

    public Task DeleteAsync(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith($"{RequestPath}/", StringComparison.Ordinal))
            return Task.CompletedTask;

        var fileName = Path.GetFileName(imageUrl);
        var path = Path.Combine(_storagePath, fileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private static string GetValidatedExtension(IBrowserFile image)
    {
        if (image.Size <= 0 || image.Size > MaxFileSize)
            throw new InvalidOperationException("Choose an image no larger than 5 MB.");

        return image.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new InvalidOperationException("Choose a JPG, PNG, or WebP image.")
        };
    }

    private static bool HasExpectedSignature(string path, string extension)
    {
        Span<byte> header = stackalloc byte[12];
        using var stream = File.OpenRead(path);
        var length = stream.Read(header);
        return extension switch
        {
            ".jpg" => length >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            ".png" => length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
            ".webp" => length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };
    }
}
