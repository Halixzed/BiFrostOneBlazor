namespace PortfolioApp.Models;

/// <summary>
/// Singleton uploaded equirectangular image used as the 3D scene's image-based lighting
/// environment, replacing the built-in studio light rig when present.
/// </summary>
public class EnvironmentMap
{
    public int Id { get; set; }
    public required string OriginalFileName { get; set; }

    /// <summary>
    /// Physical file name on disk under App_Data/uploads/environment, e.g. "environment.jpg".
    /// </summary>
    public required string StoredFileName { get; set; }
}
