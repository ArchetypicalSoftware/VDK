namespace Vdk.Services;

public interface IFluxClient
{
    void Bootstrap(string clusterName, string path, string branch = FluxClient.DefaultBranch);
}