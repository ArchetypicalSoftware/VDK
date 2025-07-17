namespace Vdk.Constants;

public static class Containers
{
    public const string RegistryName = "vega-registry";
    public const string RegistryImage = "registry:2";
    public const int RegistryContainerPort = 5000;
    public const int RegistryHostPort = 50000;
    public const string ProxyName = "vega-proxy";
    public const string ProxyImage = "nginx:latest";
    public const string CloudProviderKindName = "cloud-provider-kind";
    public const string CloudProviderKindImage = "registry.k8s.io/cloud-provider-kind/cloud-controller-manager:v0.6.0";
}