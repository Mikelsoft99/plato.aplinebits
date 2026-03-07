namespace AlpineBits.GuestRequestProxy.Data.Entities;

public sealed class GuestRequestLog
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? RequestJson { get; set; }
    public string? RequestXml { get; set; }
    public string? ResponseBody { get; set; }
    public int StatusCode { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processed, Failed
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }

    public HotelTenant? Tenant { get; set; }
}
