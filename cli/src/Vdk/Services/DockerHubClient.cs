using Vdk.Constants;
using Vdk.Models;

namespace Vdk.Services;

public class DockerHubClient : IHubClient
{
    private readonly IDockerEngine _docker;
    private readonly IConsole _console;

    public DockerHubClient(IDockerEngine docker, IConsole console)
    {
        _docker = docker;
        _console = console;
    }
    public void Create()
    {
        if (!_docker.Exists(Containers.RegistryName))
        {
            _console.WriteLine("Creating Vega VDK Registry");
            _console.WriteLine(" - This may take a few minutes...");
            _docker.Run(Containers.RegistryImage,
                Containers.RegistryName,
                [PortMapping.DefaultRegistryPortMapping],
                null,
                null,
                null);
        }
    }

    public void Destroy()
    {
        if (_docker.Exists(Containers.RegistryName))
        {
            _console.WriteWarning("Deleting Vega VDK Registry from Docker");
            _console.WriteLine("You can recreate the registry using command 'vega create registry'");
            _docker.Delete(Containers.RegistryName);
        }
    }
}