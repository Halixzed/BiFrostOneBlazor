using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

public class AppSettingsStore
{
    private readonly IDbContextFactory<PortfolioDbContext> _contextFactory;

    public AppSettingsStore(IDbContextFactory<PortfolioDbContext> contextFactory)
    {
        _contextFactory = contextFactory;

        using var context = _contextFactory.CreateDbContext();
        if (!context.AppSettings.Any())
        {
            context.AppSettings.Add(new AppSettings());
            context.SaveChanges();
        }
    }

    public event Action? Changed;

    public AppSettings Get()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.AppSettings.AsNoTracking().First();
    }

    public void SetBackgroundColor(string color)
    {
        using var context = _contextFactory.CreateDbContext();
        var settings = context.AppSettings.First();
        settings.BackgroundColor = color;
        context.SaveChanges();
        Changed?.Invoke();
    }
}
