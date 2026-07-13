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

    public event Action? Changed;

    public Watermark? Get()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Watermarks.AsNoTracking().FirstOrDefault();
    }

    // Served by a dedicated middleware in Program.cs (not a static wwwroot path), since the file
    // lives outside wwwroot and is only ever this one singleton.
    public string Url(Watermark watermark) => "/watermark-image";

    public string GetFullPath(Watermark watermark) => Path.Combine(_uploadsDirectory, watermark.StoredFileName);

    public async Task SetAsync(string originalFileName, Stream content)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only PNG or SVG files are supported.");
        }

        using var context = _contextFactory.CreateDbContext();
        var existing = context.Watermarks.FirstOrDefault();
        var grayscale = existing?.Grayscale ?? true;
        if (existing is not null)
        {
            DeleteFile(existing.StoredFileName);
            context.Watermarks.Remove(existing);
        }

        var storedFileName = $"watermark{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(_uploadsDirectory, storedFileName);
        Directory.CreateDirectory(_uploadsDirectory);
        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream);
        }

        context.Watermarks.Add(new Watermark { OriginalFileName = originalFileName, StoredFileName = storedFileName, Grayscale = grayscale });
        context.SaveChanges();
        Changed?.Invoke();
    }

    public void SetGrayscale(bool grayscale)
    {
        using var context = _contextFactory.CreateDbContext();
        var existing = context.Watermarks.FirstOrDefault();
        if (existing is null)
        {
            return;
        }

        existing.Grayscale = grayscale;
        context.SaveChanges();
        Changed?.Invoke();
    }

    public void Delete()
    {
        using var context = _contextFactory.CreateDbContext();
        var existing = context.Watermarks.FirstOrDefault();
        if (existing is null)
        {
            return;
        }

        DeleteFile(existing.StoredFileName);
        context.Watermarks.Remove(existing);
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
