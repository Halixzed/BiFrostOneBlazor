using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

public class DeviceStore
{
    private readonly IDbContextFactory<PortfolioDbContext> _contextFactory;

    public DeviceStore(IDbContextFactory<PortfolioDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Fired with the affected device's Id - subscribers (Home/Settings) filter to the device
    /// they're currently showing/editing, so a change to one tablet doesn't re-render another.
    /// </summary>
    public event Action<int>? Changed;

    public IReadOnlyList<Device> GetAll()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Devices.AsNoTracking().OrderBy(d => d.Id).ToList();
    }

    public Device? GetById(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Devices.AsNoTracking().FirstOrDefault(d => d.Id == id);
    }

    /// <summary>
    /// Loads the device with this key, transparently creating it (with sensible defaults) if it
    /// doesn't exist yet - so visiting a brand new tablet URL like /d/lobby-1 just works without a
    /// separate provisioning step.
    /// </summary>
    public Device GetOrCreateByKey(string key)
    {
        using var context = _contextFactory.CreateDbContext();
        var existing = context.Devices.AsNoTracking().FirstOrDefault(d => d.Key == key);
        if (existing is not null)
        {
            return existing;
        }

        var device = new Device { Key = key, DisplayName = key == "default" ? "Default" : key };
        context.Devices.Add(device);
        context.SaveChanges();
        return device;
    }

    public void Rename(int deviceId, string displayName)
    {
        using var context = _contextFactory.CreateDbContext();
        var device = context.Devices.FirstOrDefault(d => d.Id == deviceId);
        if (device is null)
        {
            return;
        }

        device.DisplayName = displayName;
        context.SaveChanges();
        Changed?.Invoke(deviceId);
    }

    public void SetActiveUnit(int deviceId, int? unitId)
    {
        using var context = _contextFactory.CreateDbContext();
        var device = context.Devices.FirstOrDefault(d => d.Id == deviceId);
        if (device is null)
        {
            return;
        }

        device.ActiveUnitId = unitId;
        context.SaveChanges();
        Changed?.Invoke(deviceId);
    }

    public void SetBackgroundColor(int deviceId, string color)
    {
        using var context = _contextFactory.CreateDbContext();
        var device = context.Devices.FirstOrDefault(d => d.Id == deviceId);
        if (device is null)
        {
            return;
        }

        device.BackgroundColor = color;
        context.SaveChanges();
        Changed?.Invoke(deviceId);
    }

    /// <summary>
    /// Clears ActiveUnitId on any device currently pointing at this unit - called when a unit is
    /// deleted so no device is left referencing a unit that no longer exists.
    /// </summary>
    public void ClearActiveUnitReferences(int unitId)
    {
        using var context = _contextFactory.CreateDbContext();
        var affected = context.Devices.Where(d => d.ActiveUnitId == unitId).ToList();
        foreach (var device in affected)
        {
            device.ActiveUnitId = null;
        }

        if (affected.Count > 0)
        {
            context.SaveChanges();
            foreach (var device in affected)
            {
                Changed?.Invoke(device.Id);
            }
        }
    }

    public bool Delete(int deviceId)
    {
        using var context = _contextFactory.CreateDbContext();
        var device = context.Devices.FirstOrDefault(d => d.Id == deviceId);
        if (device is null || device.Key == "default")
        {
            // The default device backs "/" and "/settings" - deleting it would break those
            // well-known routes, so it's not removable from the UI.
            return false;
        }

        context.Devices.Remove(device);
        context.SaveChanges();
        return true;
    }
}
