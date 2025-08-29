using Vdk.Models;

namespace Vdk.Services;

public interface ITokenStore
{
    Task<AuthTokens?> LoadAsync(string profile, CancellationToken ct = default);
    Task SaveAsync(string profile, AuthTokens tokens, CancellationToken ct = default);
    Task DeleteAsync(string profile, CancellationToken ct = default);
    string GetCurrentProfile();
    void SetCurrentProfile(string profile);
    IEnumerable<string> ListProfiles();
}
