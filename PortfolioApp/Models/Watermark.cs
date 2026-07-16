namespace PortfolioApp.Models;

public class Watermark
{
    public int Id { get; set; }

    /// <summary>
    /// The device this watermark belongs to - each device has at most one.
    /// </summary>
    public int DeviceId { get; set; }

    public required string OriginalFileName { get; set; }

    /// <summary>
    /// Physical file name on disk under App_Data/uploads, e.g. "watermark.png".
    /// </summary>
    public required string StoredFileName { get; set; }

    public bool Grayscale { get; set; } = true;
}
