using System.Text.Json;
using Vdk.Models;

namespace Vdk.Services;

public class TokenStoreFile : ITokenStore
{
    private readonly GlobalConfiguration _config;

    private record Persisted(AuthTokens Tokens);

    public TokenStoreFile(GlobalConfiguration config)
    {
        _config = config;
        Directory.CreateDirectory(TokensDirectory);
    }

    private string TokensDirectory => Path.Combine(_config.VegaDirectory, "tokens");
    private string ProfilesFile => Path.Combine(TokensDirectory, ".current_profile");
    private string ProfilePath(string profile) => Path.Combine(TokensDirectory, $"{profile}.json");

    public async Task<AuthTokens?> LoadAsync(string profile, CancellationToken ct = default)
    {
        var path = ProfilePath(profile);
        if (!File.Exists(path)) return null;
        await using var fs = File.OpenRead(path);
        var persisted = await JsonSerializer.DeserializeAsync<Persisted>(fs, cancellationToken: ct);
        return persisted?.Tokens;
    }

    public async Task SaveAsync(string profile, AuthTokens tokens, CancellationToken ct = default)
    {
        Directory.CreateDirectory(TokensDirectory);
        var path = ProfilePath(profile);
        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, new Persisted(tokens), options: new JsonSerializerOptions { WriteIndented = true }, cancellationToken: ct);
    }

    public Task DeleteAsync(string profile, CancellationToken ct = default)
    {
        var path = ProfilePath(profile);
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(ProfilesFile)) File.Delete(ProfilesFile);
        return Task.CompletedTask;
    }

    public string GetCurrentProfile()
    {
        if (File.Exists(ProfilesFile))
        {
            var p = File.ReadAllText(ProfilesFile).Trim();
            if (!string.IsNullOrWhiteSpace(p)) return p;
        }
        return "default";
    }

    public void SetCurrentProfile(string profile)
    {
        Directory.CreateDirectory(TokensDirectory);
        File.WriteAllText(ProfilesFile, profile);
    }

    public IEnumerable<string> ListProfiles()
    {
        if (!Directory.Exists(TokensDirectory)) yield break;
        foreach (var file in Directory.EnumerateFiles(TokensDirectory, "*.json"))
        {
            yield return Path.GetFileNameWithoutExtension(file);
        }
    }
}
