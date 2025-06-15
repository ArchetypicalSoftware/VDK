namespace Vdk.Services;

public interface IHubClient
{
    void CreateRegistry();

    void CreateCloudProviderKind();

    void DestroyRegistry();
    
    void DestroyCloudProviderKind();
    
    bool ExistRegistry();
    
    bool ExistCloudProviderKind();
}