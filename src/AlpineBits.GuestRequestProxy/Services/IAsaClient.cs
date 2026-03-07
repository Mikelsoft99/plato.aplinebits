namespace AlpineBits.GuestRequestProxy.Services;

public interface IAsaClient
{
    Task<(int StatusCode, string ResponseBody, string? ContentType)> SendAsync(
        string targetUrl,
        string action,
        string requestXml,
        string? username,
        string? password,
        CancellationToken cancellationToken);
}
