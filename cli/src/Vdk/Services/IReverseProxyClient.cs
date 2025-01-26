namespace Vdk.Services;

public interface IReverseProxyClient
{
    void UpsertCluster(string clusterName, int targetPortHttps, int targetPortHttp);

    void DeleteCluster(string clusterName);

    void Create();

    void Delete();

    void List();
}