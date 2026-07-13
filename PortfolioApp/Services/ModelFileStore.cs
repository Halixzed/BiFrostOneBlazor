namespace PortfolioApp.Services;

public class ModelFileStore
{
    private const string UrlPrefix = "/model-file/";
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".glb", ".gltf" };

    private readonly string _uploadsDirectory;

    public ModelFileStore(IWebHostEnvironment env)
    {
        // Same reasoning as WatermarkStore: kept outside wwwroot so dotnet watch's hot-reload
        // file watcher doesn't crash when files are added/replaced/deleted at runtime.
        _uploadsDirectory = Path.Combine(env.ContentRootPath, "App_Data", "uploads", "models");
        Directory.CreateDirectory(_uploadsDirectory);
    }

    public async Task<string> SaveAsync(string originalFileName, Stream content)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only GLB or GLTF files are supported.");
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
    /// Resolves a requested file name (from the /model-file/{fileName} route) to a full path,
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
