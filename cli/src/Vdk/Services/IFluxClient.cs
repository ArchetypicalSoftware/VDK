namespace Vdk.Services;

public interface IFluxClient
{
    void Bootstrap(string path, string branch = FluxClient.DefaultBranch);
}