namespace Vdk.Constants;

public static class Containers
{
    public const string RegistryName = "vega-registry";
    public const string RegistryImage = "ghcr.io/project-zot/zot:v2.1.0";
    public const int RegistryContainerPort = 5000;
    public const int RegistryHostPort = 5000;
    public const string ProxyName = "vega-proxy";
    public const string ProxyImage = "nginx:1.27";
    public const string CloudProviderKindName = "cloud-provider-kind";
    public const string CloudProviderKindImage = "registry.k8s.io/cloud-provider-kind/cloud-controller-manager:v0.6.0";
}