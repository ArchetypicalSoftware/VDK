using SemVersion;

namespace Vdk.Models;

public class KubeImage
{
    public string Version { get; set; } = string.Empty; // SemanticVersion.BaseVersion();

    public SemanticVersion SemanticVersion =>
        SemanticVersion.TryParse(Version, out var semver) ? semver : SemanticVersion.BaseVersion();
    public string Image { get; set; } = string.Empty;
}