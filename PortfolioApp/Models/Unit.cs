using BlazorThreeJS.Maths;

namespace PortfolioApp.Models;

public class Unit
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public Vector3 Rotation { get; set; } = new();

    public int Zones { get; set; }
    public int WidthPerZone { get; set; }
    public int Height { get; set; }
    public int AveragePowerUsage { get; set; }
    public int WarrantyYears { get; set; }

    /// <summary>
    /// Only one unit at a time is shown on the main viewer; toggling one active clears the rest.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Path to the uploaded .gltf/.glb model, served via the /model-file/{fileName} endpoint.
    /// Units without one render nothing in the viewer.
    /// </summary>
    public string? ModelUrl { get; set; }

    /// <summary>
    /// Original uploaded file name, kept only for display in the Settings admin UI.
    /// </summary>
    public string? ModelFileName { get; set; }

    /// <summary>
    /// Path to the uploaded PDF for the "Learn More" button, served via /pdf-file/{fileName}.
    /// Units without one don't show the button.
    /// </summary>
    public string? PdfUrl { get; set; }

    /// <summary>
    /// Original uploaded PDF file name, kept only for display in the Settings admin UI.
    /// </summary>
    public string? PdfFileName { get; set; }
}
