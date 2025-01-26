using System.Net;
using System.Net.Sockets;
using Vdk.Constants;

namespace Vdk.Models;

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

    public static PortMapping DefaultRegistryPortMapping = new PortMapping { ContainerPort =Containers.RegistryContainerPort, HostPort = Containers.RegistryHostPort };

}