namespace PortfolioApp.Services;

public class PdfFileStore
{
    private const string UrlPrefix = "/pdf-file/";
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf" };

    private readonly string _uploadsDirectory;

    public PdfFileStore(IWebHostEnvironment env)
    {
        // Same reasoning as ModelFileStore/WatermarkStore: kept outside wwwroot so dotnet watch's
        // hot-reload file watcher doesn't crash when files are added/replaced/deleted at runtime.
        _uploadsDirectory = Path.Combine(env.ContentRootPath, "App_Data", "uploads", "pdfs");
        Directory.CreateDirectory(_uploadsDirectory);
    }

    public async Task<string> SaveAsync(string originalFileName, Stream content)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only PDF files are supported.");
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        Directory.CreateDirectory(_uploadsDirectory);
        var fullPath = Path.Combine(_uploadsDirectory, storedFileName);
        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream);
        }

        return UrlPrefix + storedFileName;
    }

    public void DeleteByUrl(string? url)
    {
        var fileName = ExtractFileName(url);
        if (fileName is null)
        {
            return;
        }

        var fullPath = Path.Combine(_uploadsDirectory, fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    /// <summary>
    /// Resolves a requested file name (from the /pdf-file/{fileName} route) to a full path,
    /// confined to the uploads directory and only if the file actually exists.
    /// </summary>
    public string? GetFullPath(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(_uploadsDirectory, safeName);
        return File.Exists(fullPath) ? fullPath : null;
    }

    private static string? ExtractFileName(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(UrlPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        return Path.GetFileName(url[UrlPrefix.Length..]);
    }
}
