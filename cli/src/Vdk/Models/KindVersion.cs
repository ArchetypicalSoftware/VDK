using System.Text.Json.Serialization;
using SemVersion;

namespace Vdk.Models;

public class KindVersion
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = ""; // SemanticVersion.BaseVersion();

    [JsonIgnore]
    public SemanticVersion SemanticVersion =>
        SemanticVersion.TryParse(Version, out var semver) ? semver : SemanticVersion.BaseVersion();

    [JsonPropertyName("images")]
    public List<KubeImage> Images { get; set; } = new();
}