using Vdk.Models;

namespace Vdk.Services;

public interface IKindClient
{
    string? GetVersion();

    string GetVersionString();

    void CreateCluster(string name, string configPath);

    void DeleteCluster(string name);

    List<(string name, bool isVdk, KindNode? master)> ListClusters();
}