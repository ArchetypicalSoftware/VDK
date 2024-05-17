namespace Vdk.Services;

public interface IKindClient
{
    string? GetVersion();
    string GetVersionString();
    void CreateCluster(string name, string configPath);
}