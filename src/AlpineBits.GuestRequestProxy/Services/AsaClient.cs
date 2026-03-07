using System.Net.Http.Headers;

namespace AlpineBits.GuestRequestProxy.Services;

public sealed class AsaClient(HttpClient httpClient) : IAsaClient
{
    public async Task<(int StatusCode, string ResponseBody, string? ContentType)> SendAsync(
        string targetUrl,
        string action,
        string requestXml,
        string? username,
        string? password,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(action), "action");
        content.Add(new StringContent(requestXml), "request");
        request.Content = content;

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            var raw = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", raw);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString();

        return ((int)response.StatusCode, body, contentType);
    }
}
