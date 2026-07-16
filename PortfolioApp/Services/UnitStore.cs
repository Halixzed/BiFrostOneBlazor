using BlazorThreeJS.Maths;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

public class UnitStore
{
    private readonly IDbContextFactory<PortfolioDbContext> _contextFactory;
    private readonly DeviceStore _deviceStore;

    public UnitStore(IDbContextFactory<PortfolioDbContext> contextFactory, DeviceStore deviceStore)
    {
        _contextFactory = contextFactory;
        _deviceStore = deviceStore;

        using var context = _contextFactory.CreateDbContext();

        if (!context.Units.Any())
        {
            context.Units.AddRange(
                new Unit { Name = "core" },
                new Unit { Name = "orbiter-1" },
                new Unit { Name = "orbiter-2" },
                new Unit { Name = "orbiter-3" });
            context.SaveChanges();
        }
    }

    public event Action? Changed;

    public IReadOnlyList<Unit> GetAll()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Units.AsNoTracking().OrderBy(u => u.Id).ToList();
    }

    public Unit? GetById(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Units.AsNoTracking().FirstOrDefault(u => u.Id == id);
    }

    public Unit Add(Unit unit)
    {
        using var context = _contextFactory.CreateDbContext();
        context.Units.Add(unit);
        context.SaveChanges();
        Changed?.Invoke();
        return unit;
    }

    public bool Update(Unit unit)
    {
        using var context = _contextFactory.CreateDbContext();
        var existing = context.Units.FirstOrDefault(u => u.Id == unit.Id);
        if (existing is null)
        {
            return false;
        }

        existing.Name = unit.Name;
        existing.Rotation = new Vector3(unit.Rotation.X, unit.Rotation.Y, unit.Rotation.Z);
        existing.ModelUrl = unit.ModelUrl;
        existing.ModelFileName = unit.ModelFileName;
        existing.PdfUrl = unit.PdfUrl;
        existing.PdfFileName = unit.PdfFileName;
        existing.Zones = unit.Zones;
        existing.WidthPerZone = unit.WidthPerZone;
        existing.Height = unit.Height;
        existing.AveragePowerUsage = unit.AveragePowerUsage;
        existing.WarrantyYears = unit.WarrantyYears;

        context.SaveChanges();
        Changed?.Invoke();
        return true;
    }

    public bool Delete(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        var existing = context.Units.FirstOrDefault(u => u.Id == id);
        if (existing is null)
        {
            return false;
        }

        context.Units.Remove(existing);
        context.SaveChanges();

        // Any device currently showing this unit needs to fall back to "nothing selected" rather
        // than pointing at a unit that no longer exists.
        _deviceStore.ClearActiveUnitReferences(id);

        Changed?.Invoke();
        return true;
    }
}
