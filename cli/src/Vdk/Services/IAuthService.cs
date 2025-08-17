using Vdk.Models;

namespace Vdk.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync(string? profile = null, CancellationToken ct = default);
    Task EnsureAuthenticatedAsync(string? profile = null, CancellationToken ct = default);
    Task<AuthTokens?> GetTokensAsync(string? profile = null, CancellationToken ct = default);
    Task<string?> GetTenantIdAsync(string? profile = null, CancellationToken ct = default);
    Task LoginAsync(string? profile = null, CancellationToken ct = default);
    Task LogoutAsync(string? profile = null, CancellationToken ct = default);
    string GetCurrentProfile();
    void SetCurrentProfile(string profile);
}
