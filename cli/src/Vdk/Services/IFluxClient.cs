namespace Vdk.Services;

public interface IFluxClient
{
    void Bootstrap(string clusterName, string path, string branch = FluxClient.DefaultBranch);
    bool WaitForKustomizations(string clusterName, int maxAttempts = 60, int delaySeconds = 5);
}