namespace PortfolioApp.Models;

/// <summary>
/// Singleton row of app-wide, admin-configurable display settings.
/// </summary>
public class AppSettings
{
    public int Id { get; set; }
    public string BackgroundColor { get; set; } = "#333333";
}
