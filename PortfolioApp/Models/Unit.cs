using BlazorThreeJS.Maths;

namespace PortfolioApp.Models;

public enum UnitShape
{
    Box,
    Sphere,
    Cone,
    Torus,
}

public class Unit
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public UnitShape Shape { get; set; } = UnitShape.Box;
    public Vector3 Position { get; set; } = new();
    public Vector3 Rotation { get; set; } = new();
    public double Scale { get; set; } = 1;
    public string Color { get; set; } = "#4f9dff";
}
