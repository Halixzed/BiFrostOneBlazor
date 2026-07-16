namespace PortfolioApp.Models;

/// <summary>
/// Uploaded equirectangular image used as a device's 3D scene image-based lighting environment,
/// added on top of (not replacing) the built-in studio light rig when present. Each device has
/// at most one.
/// </summary>
public class EnvironmentMap
{
    public int Id { get; set; }

    public int DeviceId { get; set; }

    public required string OriginalFileName { get; set; }

    /// <summary>
    /// Physical file name on disk under App_Data/uploads/environment, e.g. "environment.jpg".
    /// </summary>
    public required string StoredFileName { get; set; }
}
