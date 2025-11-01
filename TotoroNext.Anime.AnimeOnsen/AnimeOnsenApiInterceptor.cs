using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using TotoroNext.Module;

namespace TotoroNext.Anime.AnimeOnsen;

public class AnimeOnsenApiInterceptor(TokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

[UsedImplicitly]
public class TokenProvider(IHttpClientFactory httpClientFactory)
{
    private string _accessToken = string.Empty;
    private DateTime _expiresAt = DateTime.MinValue;

    public async ValueTask<string> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresAt)
        {
            return _accessToken;
        }

        using var client = httpClientFactory.CreateClient();

        var json = JsonSerializer.Serialize(new
        {
            client_id = "f296be26-28b5-4358-b5a1-6259575e23b7",
            client_secret = "349038c4157d0480784753841217270c3c5b35f4281eaee029de21cb04084235",
            grant_type = "client_credentials"
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, "https://auth.animeonsen.xyz/oauth/token")
        {
            Content = content
        };
        request.Headers.Add(HeaderNames.UserAgent, Http.UserAgent);

        var response = await client.PostAsync("https://auth.animeonsen.xyz/oauth/token", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var tokenObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

        _accessToken = tokenObject.GetProperty("access_token").GetString() ?? "";
        _expiresAt = DateTime.UtcNow.AddHours(2);

        return _accessToken;
    }
}