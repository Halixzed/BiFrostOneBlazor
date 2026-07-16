namespace PortfolioApp.Models;

/// <summary>
/// A single tablet/display. Everything shown on that screen - which unit, background color,
/// watermark, HDRI - is scoped to its own Device row rather than shared globally, so changing one
/// tablet's display no longer affects any other tablet (or the admin's own browser).
/// </summary>
public class Device
{
    public int Id { get; set; }

    /// <summary>
    /// URL slug identifying this device, e.g. "lobby-1" for /d/lobby-1. The well-known key
    /// "default" is what "/" and "/settings" resolve to, so existing bookmarks/URLs keep working.
    /// </summary>
    public required string Key { get; set; }

    public string DisplayName { get; set; } = "New Display";

    /// <summary>
    /// Which unit (from the shared catalog) this device currently shows. Null if none selected yet.
    /// </summary>
    public int? ActiveUnitId { get; set; }

    public string BackgroundColor { get; set; } = "#333333";
}
