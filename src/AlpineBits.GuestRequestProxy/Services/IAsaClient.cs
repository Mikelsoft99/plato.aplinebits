namespace AlpineBits.GuestRequestProxy.Services;

public interface IAsaClient
{
    Task<(int StatusCode, string ResponseBody, string? ContentType)> ForwardGuestRequestAsync(string payload, CancellationToken cancellationToken);
}
