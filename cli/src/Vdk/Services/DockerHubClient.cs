using Vdk.Models;

namespace Vdk.Services;

public class DockerHubClient : IHubClient
{
    private readonly IDockerEngine _docker;

    public DockerHubClient(IDockerEngine docker)
    {
        _docker = docker;

        if (!_docker.Exists("registry"))
        {
            _docker.Run("registry:2",
                "registry",
                new PortMapping[] { new PortMapping() { ContainerPort = 5000, HostPort = 5000 } },
                null,
                null,
                null);
        }

    }
    public void Create()
    {
        
    }

    public void Destroy()
    {
        throw new NotImplementedException();
    }
}