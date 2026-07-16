using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

public class WatermarkStore
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".svg" };

    private readonly IDbContextFactory<PortfolioDbContext> _contextFactory;
    private readonly string _uploadsDirectory;

    public WatermarkStore(IDbContextFactory<PortfolioDbContext> contextFactory, IWebHostEnvironment env)
    {
        _contextFactory = contextFactory;
        // Deliberately outside wwwroot: dotnet watch's hot-reload file watcher can crash
        // (HotReloadMSBuildWorkspace "Unexpected true") when files change under wwwroot mid-run.
        // App_Data (which also holds the SQLite db, written on every request) isn't part of any
        // watched item glob, so it's safe for files that get added/replaced/deleted at runtime.
        _uploadsDirectory = Path.Combine(env.ContentRootPath, "App_Data", "uploads");
        Directory.CreateDirectory(_uploadsDirectory);
    }

    /// <summary>
    /// Fired with the affected device's Id - see DeviceStore.Changed for why.
    /// </summary>
    public event Action<int>? Changed;

    public Watermark? Get(int deviceId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Watermarks.AsNoTracking().FirstOrDefault(w => w.DeviceId == deviceId);
    }

    // Served by a dedicated middleware in Program.cs (not a static wwwroot path), since the file
    // lives outside wwwroot.
    public string Url(Watermark watermark) => $"/watermark-image/{watermark.DeviceId}";

    public string GetFullPath(Watermark watermark) => Path.Combine(_uploadsDirectory, watermark.StoredFileName);

    public async Task SetAsync(int deviceId, string originalFileName, Stream content)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only PNG or SVG files are supported.");
        }

        using var context = _contextFactory.CreateDbContext();
        var existing = context.Watermarks.FirstOrDefault(w => w.DeviceId == deviceId);
        var grayscale = existing?.Grayscale ?? true;
        if (existing is not null)
        {
            DeleteFile(existing.StoredFileName);
            context.Watermarks.Remove(existing);
        }

        var storedFileName = $"watermark-{deviceId}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(_uploadsDirectory, storedFileName);
        Directory.CreateDirectory(_uploadsDirectory);
        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream);
        }

        context.Watermarks.Add(new Watermark { DeviceId = deviceId, OriginalFileName = originalFileName, StoredFileName = storedFileName, Grayscale = grayscale });
        context.SaveChanges();
        Changed?.Invoke(deviceId);
    }

    public void SetGrayscale(int deviceId, bool grayscale)
    {
        using var context = _contextFactory.CreateDbContext();
        var existing = context.Watermarks.FirstOrDefault(w => w.DeviceId == deviceId);
        if (existing is null)
        {
            return;
        }

        existing.Grayscale = grayscale;
        context.SaveChanges();
        Changed?.Invoke(deviceId);
    }

    public void Delete(int deviceId)
    {
        using var context = _contextFactory.CreateDbContext();
        var existing = context.Watermarks.FirstOrDefault(w => w.DeviceId == deviceId);
        if (existing is null)
        {
            return;
        }

        DeleteFile(existing.StoredFileName);
        context.Watermarks.Remove(existing);
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
