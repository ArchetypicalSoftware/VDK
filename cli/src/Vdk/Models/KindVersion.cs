using SemVersion;

namespace Vdk.Models;

public class KindVersion
{
    public string Version { get; set; } = ""; // SemanticVersion.BaseVersion();

    public SemanticVersion SemanticVersion =>
        SemanticVersion.TryParse(Version, out var semver) ? semver : SemanticVersion.BaseVersion();
    public List<KubeImage> Images { get; set; } = new();
}