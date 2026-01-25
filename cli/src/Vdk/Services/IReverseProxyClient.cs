namespace Vdk.Services;

public interface IReverseProxyClient
{
    void UpsertCluster(string clusterName, int targetPortHttps, int targetPortHttp, bool reload = true);

    void DeleteCluster(string clusterName);

    void Create();

    void Delete();

    void List();

    bool Exists();

    /// <summary>
    /// Regenerates the nginx configuration for all VDK clusters.
    /// Useful for applying config changes (like WebSocket support) to existing clusters.
    /// </summary>
    void RegenerateConfigs();
}