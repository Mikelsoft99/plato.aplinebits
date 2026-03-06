using System.Net.Http.Headers;
using System.Text;
using AlpineBits.GuestRequestProxy.Options;
using Microsoft.Extensions.Options;

namespace AlpineBits.GuestRequestProxy.Services;

public sealed class AsaClient(HttpClient httpClient, IOptions<AlpineBitsOptions> options) : IAsaClient
{
    private readonly AlpineBitsOptions _options = options.Value;

    public async Task<(int StatusCode, string ResponseBody, string? ContentType)> ForwardGuestRequestAsync(string payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TargetUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/xml")
        };

        request.Headers.TryAddWithoutValidation("X-AlpineBits-Version", _options.Version);
        request.Headers.TryAddWithoutValidation("X-AlpineBits-Action", _options.Action);

        if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", raw);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString();

        return ((int)response.StatusCode, body, contentType);
    }
}
