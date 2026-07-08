using BlazorThreeJS.Maths;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

public class UnitStore
{
    private readonly List<Unit> _units;
    private int _nextId;

    public UnitStore()
    {
        _units =
        [
            new Unit { Name = "core", Shape = UnitShape.Box, Position = new Vector3(0, 0, 0), Color = "#4f9dff" },
            new Unit { Name = "orbiter-1", Shape = UnitShape.Sphere, Position = new Vector3(4, 1, 0), Scale = 0.6, Color = "#ff6b6b" },
            new Unit { Name = "orbiter-2", Shape = UnitShape.Cone, Position = new Vector3(-4, 0.5, 2), Scale = 0.8, Color = "#ffd166" },
            new Unit { Name = "orbiter-3", Shape = UnitShape.Torus, Position = new Vector3(0, 1, -4), Scale = 0.8, Color = "#06d6a0" },
        ];

        foreach (var unit in _units)
        {
            unit.Id = ++_nextId;
        }
    }

    public event Action? Changed;

    public IReadOnlyList<Unit> GetAll() => _units;

    public Unit? GetById(int id) => _units.FirstOrDefault(u => u.Id == id);

    public Unit Add(Unit unit)
    {
        unit.Id = ++_nextId;
        _units.Add(unit);
        Changed?.Invoke();
        return unit;
    }

    public bool Update(Unit unit)
    {
        var index = _units.FindIndex(u => u.Id == unit.Id);
        if (index < 0)
        {
            return false;
        }

        _units[index] = unit;
        Changed?.Invoke();
        return true;
    }

    public bool Delete(int id)
    {
        var removed = _units.RemoveAll(u => u.Id == id) > 0;
        if (removed)
        {
            Changed?.Invoke();
        }

        return removed;
    }
}
