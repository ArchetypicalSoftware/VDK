using Vdk.Constants;
using Vdk.Models;

namespace Vdk.Services;

public class DockerHubClient(IDockerEngine docker, IConsole console) : IHubClient
{
    public void CreateRegistry()
    {
        if (ExistRegistry()) return;
        console.WriteLine("Creating Vega VDK Registry");
        console.WriteLine(" - This may take a few minutes...");
        docker.Run(Containers.RegistryImage,
            Containers.RegistryName,
            [PortMapping.DefaultRegistryPortMapping],
            null,
            null,
            null);
    }

    public void CreateCloudProviderKind()
    {
        if (ExistCloudProviderKind()) return;
        console.WriteLine("Creating Cloud Provider Kind");
        // docker run -d --name cloud-provider-kind --network kind -v /var/run/docker.sock:/var/run/docker.sock registry.k8s.io/cloud-provider-kind/cloud-controller-manager:v0.6.0
        var volumes = new List<FileMapping>()
        {
            new()
            {
                Source = "/var/run/docker.sock",
                Destination = "/var/run/docker.sock"
            }
        };
        docker.Run(Containers.CloudProviderKindImage,
            Containers.CloudProviderKindName,
            null, null, volumes.ToArray(), null, "kind");
    }


    public void DestroyRegistry()
    {
        if (!ExistRegistry()) return;
        console.WriteWarning("Deleting Vega VDK Registry from Docker");
        console.WriteLine("You can recreate the registry using command 'vega create registry'");
        docker.Delete(Containers.RegistryName);
    }

    public void DestroyCloudProviderKind()
    {
        if (!ExistCloudProviderKind()) return;
        console.WriteWarning("Deleting Cloud Provider Kind from Docker");
        docker.Delete(Containers.CloudProviderKindName);
    }

    public bool ExistRegistry()
    {
        return docker.Exists(Containers.RegistryName);
    }

    public bool ExistCloudProviderKind()
    {
        return docker.Exists(Containers.CloudProviderKindName);
    }
}