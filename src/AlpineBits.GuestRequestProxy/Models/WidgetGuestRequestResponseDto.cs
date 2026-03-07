namespace AlpineBits.GuestRequestProxy.Models;

public sealed class WidgetGuestRequestResponseDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? RequestId { get; init; }
    public int PmsStatusCode { get; init; }
}
