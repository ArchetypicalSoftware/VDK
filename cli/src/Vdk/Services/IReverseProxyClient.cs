using Docker.DotNet;

namespace Vdk.Services;

public interface IReverseProxyClient
{
    void Upsert(string clusterName, int targetPortHttps, int targetPortHttp);

    void Delete(string clusterName);

    void List();
}