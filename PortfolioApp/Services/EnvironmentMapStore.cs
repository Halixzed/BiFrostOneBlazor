using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

public class EnvironmentMapStore
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

    private readonly IDbContextFactory<PortfolioDbContext> _contextFactory;
    private readonly string _uploadsDirectory;

    public EnvironmentMapStore(IDbContextFactory<PortfolioDbContext> contextFactory, IWebHostEnvironment env)
    {
        _contextFactory = contextFactory;
        // Same reasoning as WatermarkStore/ModelFileStore: kept outside wwwroot so dotnet watch's
        // hot-reload file watcher doesn't crash on files that change at runtime.
        _uploadsDirectory = Path.Combine(env.ContentRootPath, "App_Data", "uploads", "environment");
        Directory.CreateDirectory(_uploadsDirectory);
    }

    public event Action? Changed;

    public EnvironmentMap? Get()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.EnvironmentMaps.AsNoTracking().FirstOrDefault();
    }

    // Served by a dedicated middleware in Program.cs, since the file lives outside wwwroot and is
    // only ever this one singleton.
    public string Url(EnvironmentMap environmentMap) => "/environment-map-image";

    public string GetFullPath(EnvironmentMap environmentMap) => Path.Combine(_uploadsDirectory, environmentMap.StoredFileName);

    public async Task SetAsync(string originalFileName, Stream content)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only JPG or PNG equirectangular images are supported.");
        }

        using var context = _contextFactory.CreateDbContext();
        var existing = context.EnvironmentMaps.FirstOrDefault();
        if (existing is not null)
        {
            DeleteFile(existing.StoredFileName);
            context.EnvironmentMaps.Remove(existing);
        }

        var storedFileName = $"environment{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(_uploadsDirectory, storedFileName);
        Directory.CreateDirectory(_uploadsDirectory);
        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream);
        }

        context.EnvironmentMaps.Add(new EnvironmentMap { OriginalFileName = originalFileName, StoredFileName = storedFileName });
        context.SaveChanges();
        Changed?.Invoke();
    }

    public void Delete()
    {
        using var context = _contextFactory.CreateDbContext();
        var existing = context.EnvironmentMaps.FirstOrDefault();
        if (existing is null)
        {
            return;
        }

        DeleteFile(existing.StoredFileName);
        context.EnvironmentMaps.Remove(existing);
        context.SaveChanges();
        Changed?.Invoke();
    }

    private void DeleteFile(string storedFileName)
    {
        var fullPath = Path.Combine(_uploadsDirectory, storedFileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
