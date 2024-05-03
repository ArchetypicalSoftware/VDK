using k8s;
using k8s.Models;
using YamlDotNet.Serialization;

namespace Vdk.Models;

public class KindCluster
{
    public string ApiVersion { get; set; } = "kind.x-k8s.io/v1alpha4";
    public string Kind { get; set; } = "Cluster";

    public List<KindNode> Nodes { get; set; } = new List<KindNode>();
}

public class KindNode
{
    public string Role { get; set; } = "worker";
    public string Image { get; set; } = string.Empty;
    public Dictionary<string, string>? Labels { get; set; } = null;
    public List<PortMapping>? ExtraPortMappings { get; set; } = null;
}

public class PortMapping
{
    public int ContainerPort { get; set; } = 80;
    public int HostPort { get; set; } = 8080;
    public string Protocol { get; set; } = "TCP";

    public static PortMapping DefaultHttp => new PortMapping();
    public static PortMapping DefaultHttps => new PortMapping { ContainerPort = 443, HostPort = 4443 };
    public static List<PortMapping> Defaults => new() { DefaultHttp, DefaultHttps };
}