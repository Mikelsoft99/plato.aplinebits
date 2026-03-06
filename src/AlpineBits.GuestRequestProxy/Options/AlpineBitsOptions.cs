namespace AlpineBits.GuestRequestProxy.Options;

public sealed class AlpineBitsOptions
{
    public const string SectionName = "AlpineBits";

    public required string TargetUrl { get; init; }
    public string Version { get; init; } = "2024-10";
    public string Action { get; init; } = "GuestRequests";
    public string? Username { get; init; }
    public string? Password { get; init; }
}
