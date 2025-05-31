using Vdk.Constants;
using Vdk.Models;

namespace Vdk.Services;

public class DockerHubClient(IDockerEngine docker, IConsole console) : IHubClient
{
    public void Create()
    {
        if (Exists()) return;
        console.WriteLine("Creating Vega VDK Registry");
        console.WriteLine(" - This may take a few minutes...");
        docker.Run(Containers.RegistryImage,
            Containers.RegistryName,
            [PortMapping.DefaultRegistryPortMapping],
            null,
            null,
            null);
    }

    public void Destroy()
    {
        if (!Exists()) return;
        console.WriteWarning("Deleting Vega VDK Registry from Docker");
        console.WriteLine("You can recreate the registry using command 'vega create registry'");
        docker.Delete(Containers.RegistryName);
    }

    public bool Exists()
    {
        return docker.Exists(Containers.RegistryName);
    }
}