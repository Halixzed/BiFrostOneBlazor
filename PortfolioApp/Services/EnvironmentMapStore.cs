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

    /// <summary>
    /// Fired with the affected device's Id - see DeviceStore.Changed for why.
    /// </summary>
    public event Action<int>? Changed;

    public EnvironmentMap? Get(int deviceId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.EnvironmentMaps.AsNoTracking().FirstOrDefault(e => e.DeviceId == deviceId);
    }

    // Served by a dedicated middleware in Program.cs, since the file lives outside wwwroot.
    public string Url(EnvironmentMap environmentMap) => $"/environment-map-image/{environmentMap.DeviceId}";

    public string GetFullPath(EnvironmentMap environmentMap) => Path.Combine(_uploadsDirectory, environmentMap.StoredFileName);

    public async Task SetAsync(int deviceId, string originalFileName, Stream content)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only JPG or PNG equirectangular images are supported.");
        }

        using var context = _contextFactory.CreateDbContext();
        var existing = context.EnvironmentMaps.FirstOrDefault(e => e.DeviceId == deviceId);
        if (existing is not null)
        {
            DeleteFile(existing.StoredFileName);
            context.EnvironmentMaps.Remove(existing);
        }

        var storedFileName = $"environment-{deviceId}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(_uploadsDirectory, storedFileName);
        Directory.CreateDirectory(_uploadsDirectory);
        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream);
        }

        context.EnvironmentMaps.Add(new EnvironmentMap { DeviceId = deviceId, OriginalFileName = originalFileName, StoredFileName = storedFileName });
        context.SaveChanges();
        Changed?.Invoke(deviceId);
    }

    public void Delete(int deviceId)
    {
        using var context = _contextFactory.CreateDbContext();
        var existing = context.EnvironmentMaps.FirstOrDefault(e => e.DeviceId == deviceId);
        if (existing is null)
        {
            return;
        }

        DeleteFile(existing.StoredFileName);
        context.EnvironmentMaps.Remove(existing);
        context.SaveChanges();
        Changed?.Invoke(deviceId);
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
