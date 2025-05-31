using System.Text.Json.Serialization;
using SemVersion;

namespace Vdk.Models;

public class KubeImage
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty; // SemanticVersion.BaseVersion();

    [JsonIgnore]
    public SemanticVersion SemanticVersion =>
        SemanticVersion.TryParse(Version, out var semver) ? semver : SemanticVersion.BaseVersion();

    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;
}