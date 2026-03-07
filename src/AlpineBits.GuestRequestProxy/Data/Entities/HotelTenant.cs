namespace AlpineBits.GuestRequestProxy.Data.Entities;

public sealed class HotelTenant
{
    public int Id { get; set; }
    public string HotelCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<GuestRequestLog> GuestRequestLogs { get; set; } = [];
}
