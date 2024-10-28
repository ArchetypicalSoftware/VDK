using Docker.DotNet;
using Vdk.Models;

namespace Vdk.Services;

public interface IDockerEngine
{
    bool Run(string image, string name, PortMapping[]? ports, Dictionary<string, string>? envs, FileMapping[]? volumes, string[]? commands);

    bool Exists(string name);

    bool Delete(string name);

    bool Stop(string name);

    bool Exec(string name, string[] commands);
}

public interface IReverseProxyClient
{
    void Upsert(string clusterName, int targetPortHttps, int targetPortHttp);

    void Delete(string clusterName);

    void List();
}
