using System.Text.Json;
using System.Text;
using System.Linq;
using Vdk.Models;

namespace Vdk.Services;

public class AuthService : IAuthService
{
    private readonly ITokenStore _store;
    private readonly HydraDeviceFlowClient _hydra;
    private readonly GlobalConfiguration _config;
    private readonly IConsole _console;

    public AuthService(ITokenStore store, HydraDeviceFlowClient hydra, GlobalConfiguration config, IConsole console)
    {
        _store = store;
        _hydra = hydra;
        _config = config;
        _console = console;
    }

    public string GetCurrentProfile() => _store.GetCurrentProfile();

    public void SetCurrentProfile(string profile) => _store.SetCurrentProfile(profile);

    public async Task<bool> IsAuthenticatedAsync(string? profile = null, CancellationToken ct = default)
    {
        var tokens = await GetTokensAsync(profile, ct);
        return tokens is not null;
    }

    public async Task EnsureAuthenticatedAsync(string? profile = null, CancellationToken ct = default)
    {
        var tokens = await GetTokensAsync(profile, ct);
        if (tokens is null)
        {
            _console.WriteLine("You are not logged in. Starting device login...");
            await LoginAsync(profile, ct);
        }
    }

    public async Task<AuthTokens?> GetTokensAsync(string? profile = null, CancellationToken ct = default)
    {
        profile ??= GetCurrentProfile();
        var tokens = await _store.LoadAsync(profile, ct);
        if (tokens is null) return null;
        if (DateTimeOffset.UtcNow >= tokens.ExpiresAt)
        {
            if (string.IsNullOrWhiteSpace(tokens.RefreshToken)) return null;
            try
            {
                tokens = await _hydra.RefreshAsync(tokens.RefreshToken, ct);
                await _store.SaveAsync(profile, tokens, ct);
            }
            catch
            {
                return null;
            }
        }
        return tokens;
    }

    public async Task<string?> GetTenantIdAsync(string? profile = null, CancellationToken ct = default)
    {
        var tokens = await GetTokensAsync(profile, ct);
        if (tokens is null) return null;
        return ExtractClaim(tokens.AccessToken, _config.TenantIdClaim);
    }

    public async Task LoginAsync(string? profile = null, CancellationToken ct = default)
    {
        profile ??= GetCurrentProfile();
        var (deviceCode, userCode, verificationUri, complete, interval) = await _hydra.BeginAsync(ct);
        _console.WriteLine($"To sign in, visit: {complete}");
        _console.WriteLine($"Device code: {userCode}");
        var tokens = await _hydra.PollForTokenAsync(deviceCode, interval, ct);
        await _store.SaveAsync(profile, tokens, ct);
        _console.WriteLine("Login successful.");
    }

    public async Task LogoutAsync(string? profile = null, CancellationToken ct = default)
    {
        profile ??= GetCurrentProfile();
        await _store.DeleteAsync(profile, ct);
        _console.WriteLine("Logged out. Local credentials removed.");
    }

    private static string? ExtractClaim(string jwt, string claimName)
    {
        try
        {
            var parts = jwt.Split('.')
                           .Select(p => p.Replace('-', '+').Replace('_', '/'))
                           .ToArray();
            if (parts.Length < 2) return null;
            var padded = parts[1].PadRight(parts[1].Length + ((4 - parts[1].Length % 4) % 4), '=');
            var payloadBytes = Convert.FromBase64String(padded);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty(claimName, out var el))
            {
                return el.GetString();
            }
        }
        catch { }
        return null;
    }
}
