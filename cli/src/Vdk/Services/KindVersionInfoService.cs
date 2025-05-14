using System;
using System.IO.Abstractions;
using Octokit;
using Vdk.Constants;
using Vdk.Data;
using Vdk.Models;

namespace Vdk.Services;

public class KindVersionInfoService(IConsole console, IFileSystem fileSystem, IGitHubClient gitHub, IEmbeddedDataReader dataReader, 
    IJsonObjectSerializer serializer, GlobalConfiguration configuration) : IKindVersionInfoService
{

    private KindVersionMap? _cache = null;
    protected string KindVersionInfoFilePath => configuration.KindVersionInfoFilePath;

    public async Task<KindVersionMap?> UpdateAsync()
    {
        const int count = 100;
        try
        {
            var kindVersions = (await FetchKindVersionsAsync())
                .Where(kv => kv.Images.Count > 0 && !string.IsNullOrWhiteSpace(kv.Version))
                .OrderByDescending(kv =>
                {
                    if (Version.TryParse(kv.Version, out var v))
                        return v;
                    // fallback for non-standard version strings
                    return new Version(0, 0);
                })
                .ThenByDescending(kv => kv.Version, StringComparer.OrdinalIgnoreCase)
                .Take(count)
                .ToList();
            var result = new KindVersionMap(kindVersions)
            {
                LastUpdated = DateTime.Now
            };
            _cache = result;
            var json = serializer.Serialize(kindVersions);
            var file = fileSystem.FileInfo.New(KindVersionInfoFilePath);
            if (!file.Directory!.Exists)
            {
                console.WriteLine($"Creating config directory '{file.Directory.FullName}'");
                file.Directory.Create();
            }
            using (var writer = file.CreateText())
            {
                console.WriteLine($"Saving {file.FullName}");
                await writer.WriteAsync(json);
            }

            return result;
        }
        catch (Exception ex)
        {
            console.WriteWarning($"Error: {ex.Message}");
            return null;
        }
    }

    public async Task<KindVersionMap> GetVersionInfoAsync()
    {
        var file = fileSystem.FileInfo.New(KindVersionInfoFilePath);
        KindVersionMap? result = _cache;
        if (result is null && file.Exists)
        {
            using (var reader = file.OpenText())
            {
                var json = await reader.ReadToEndAsync();
                result = serializer.Deserialize<KindVersionMap>(json);
            }
        }

        if (result is null || DateTime.Now.Subtract(result.LastUpdated) > TimeSpan.FromMinutes(Defaults.KindVersionInfoCacheMinutes))
        {
            result = await UpdateAsync();
        }

        if (result is null)
        {
            result = dataReader.ReadJsonObjects<KindVersionMap>("Vdk.Data.KindVersionData.json");
        }

        return result;
    }

    public async Task<string> GetDefaultKubernetesVersionAsync(string kindVersion)
    {
        var map = await GetVersionInfoAsync();
        var version = map.SingleOrDefault(x => x.Version.Equals(kindVersion, StringComparison.CurrentCultureIgnoreCase))?
            .Images.OrderByDescending(x => x.SemanticVersion).FirstOrDefault()?.Image ?? Defaults.KubeApiVersion;
        return version;
    }

    private async Task<KindVersionMap> FetchKindVersionsAsync()
    {
        console.WriteLine("Retrieving Kind Release Data...");
        var releases = await gitHub.Repository.Release.GetAll("kubernetes-sigs", "kind");
        var kindVersions = new KindVersionMap();
        foreach (var release in releases)
        {
            if (release.TagName.StartsWith("v"))
            {
                var version = release.TagName.TrimStart('v');
                var images = new List<KubeImage>();
                // Try to extract image info from release body
                if (!string.IsNullOrWhiteSpace(release.Body))
                {
                    images.AddRange(ParseImagesFromBody(release.Body));
                }
                if (images.Count > 0)
                {
                    kindVersions.Add(new KindVersion { Version = version, Images = images });
                }
            }
        }
        console.WriteLine($"Retrieved {kindVersions.Count} Releases (Latest Release: {kindVersions.Max(x=>x.SemanticVersion)})");
        return kindVersions;
    }

    public static List<KubeImage> ParseImagesFromBody(string body)
    {
        var images = new Dictionary<string, KubeImage>();
        var lines = body.Split('\n');
        var imageLineRegex = new System.Text.RegularExpressions.Regex(@"kindest/node:v(?<version>[\d.]+)@sha256:(?<sha>[a-f0-9]+)");
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var match = imageLineRegex.Match(line);
            if (match.Success)
            {
                var version = match.Groups["version"].Value;
                var sha = match.Groups["sha"].Value;
                var imageString = $"kindest/node:v{version}@sha256:{sha}";
                // Deduplicate by version and image string
                if (!images.ContainsKey(version))
                {
                    images[version] = new KubeImage
                    {
                        Version = version,
                        Image = imageString
                    };
                }
            }
        }
        // Deduplicate by version and image string using LINQ
        return images.Values
            .GroupBy(img => new { img.Version, img.Image })
            .Select(g => g.First())
            .ToList();
    }
}