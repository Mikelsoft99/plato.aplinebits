namespace AlpineBits.GuestRequestProxy.Options;

public sealed class PmsAccountsOptions
{
    public const string SectionName = "PmsAccounts";

    public List<PmsAccount> Accounts { get; init; } = [];
}

public sealed class PmsAccount
{
    public required string HotelCode { get; init; }
    public required string ApiKey { get; init; }
    public required string TargetUrl { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
}
