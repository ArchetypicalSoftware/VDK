using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using Vdk.Models;

namespace Vdk.Services;

public class HydraDeviceFlowClient
{
    private readonly HttpClient _http;
    private readonly GlobalConfiguration _config;

    public HydraDeviceFlowClient(HttpClient http, GlobalConfiguration config)
    {
        _http = http;
        _config = config;
    }

    private record DeviceAuthResponse(string device_code, string user_code, string verification_uri, string verification_uri_complete, int expires_in, int interval);
    private record TokenResponse(string access_token, string? refresh_token, int expires_in, string token_type, string? id_token);

    public async Task<(string deviceCode, string userCode, string verificationUri, string verificationUriComplete, int interval)> BeginAsync(CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _config.OAuthClientId,
            ["scope"] = string.Join(' ', _config.OAuthScopes)
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, _config.HydraDeviceAuthorizationEndpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var data = await JsonSerializer.DeserializeAsync<DeviceAuthResponse>(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct) 
                   ?? throw new InvalidOperationException("Invalid device auth response");
        return (data.device_code, data.user_code, data.verification_uri, data.verification_uri_complete, data.interval);
    }

    public async Task<AuthTokens> PollForTokenAsync(string deviceCode, int intervalSeconds, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                ["device_code"] = deviceCode,
                ["client_id"] = _config.OAuthClientId
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, _config.HydraTokenEndpoint)
            {
                Content = new FormUrlEncodedContent(form)
            };
            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (resp.IsSuccessStatusCode)
            {
                var tok = JsonSerializer.Deserialize<TokenResponse>(body) ?? throw new InvalidOperationException("Invalid token response");
                return new AuthTokens
                {
                    AccessToken = tok.access_token,
                    RefreshToken = tok.refresh_token ?? string.Empty,
                    ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tok.expires_in - 30)
                };
            }
            if (body.Contains("authorization_pending") || body.Contains("slow_down"))
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);
                continue;
            }
            throw new InvalidOperationException($"Device code flow failed: {body}");
        }
    }

    public async Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = _config.OAuthClientId
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, _config.HydraTokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var tok = await JsonSerializer.DeserializeAsync<TokenResponse>(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct)
                  ?? throw new InvalidOperationException("Invalid token response");
        return new AuthTokens
        {
            AccessToken = tok.access_token,
            RefreshToken = tok.refresh_token ?? refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tok.expires_in - 30)
        };
    }
}
