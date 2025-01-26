namespace Vdk.Models;

public class KindCluster
{
    public string ApiVersion { get; set; } = "kind.x-k8s.io/v1alpha4";
    public string Kind { get; set; } = "Cluster";

    public List<KindNode> Nodes { get; set; } = new List<KindNode>();

    public string[] ContainerdConfigPatches { get; set; } = Array.Empty<string>();
}

public class KindNode
{
    public string Role { get; set; } = "worker";
    public string Image { get; set; } = string.Empty;
    public Dictionary<string, string>? Labels { get; set; } = null;
    public List<PortMapping>? ExtraPortMappings { get; set; } = null;
    public List<Mount>? ExtraMounts { get; set; } = null;
}

public class FileMapping
{
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
}

public class Mount
{
    public string HostPath { get; set; } = string.Empty;
    public string ContainerPath { get; set; } = string.Empty;
}