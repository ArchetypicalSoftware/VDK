using System.Text.Json.Serialization;
using System.Xml.Serialization;
using YamlDotNet.Serialization;

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

    [JsonIgnore]
    [YamlIgnore]
    public int? HttpsHostPort => ExtraPortMappings?.FirstOrDefault(x => x.ContainerPort == 30443)?.HostPort;

    [YamlIgnore]
    [JsonIgnore]
    public int? HttpHostPort => ExtraPortMappings?.FirstOrDefault(x => x.ContainerPort == 30080)?.HostPort;
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