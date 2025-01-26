using System.Net.Sockets;
using System.Net;

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

public class PortMapping
{
    public int ContainerPort { get; set; } = 80;
    public int HostPort { get; set; } = 80;
    public string Protocol { get; set; } = "TCP";

    public string ListenAddress { get; set; } = "127.0.0.1";

    public static PortMapping DefaultHttp => new PortMapping { ContainerPort = 30080, HostPort = GetRandomUnusedPort() };
    public static PortMapping DefaultHttps => new PortMapping { ContainerPort = 30443, HostPort = GetRandomUnusedPort() };
    public static List<PortMapping> Defaults => [DefaultHttps, DefaultHttp];

    internal static int GetRandomUnusedPort()
    {
        using var tcpListener = new TcpListener(IPAddress.Any, 0);
        tcpListener.Start();
        var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
        tcpListener.Stop();
        return port;
    }
}