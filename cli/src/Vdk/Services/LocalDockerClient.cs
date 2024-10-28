using Docker.DotNet.Models;
using Vdk.Models;

namespace Vdk.Services;

public class LocalDockerClient : IDockerEngine
{
    private readonly Docker.DotNet.IDockerClient _dockerClient;

    public LocalDockerClient(Docker.DotNet.IDockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public bool Run(string image, string name, PortMapping[]? ports, Dictionary<string, string>? envs, FileMapping[]? volumes, string[]? commands)
    {
        _dockerClient.Images.CreateImageAsync(
          new ImagesCreateParameters
          {
              FromImage = image,
          },
          null,
          new Progress<JSONMessage>()).GetAwaiter().GetResult();

        var response = _dockerClient.Containers.CreateContainerAsync(
            new CreateContainerParameters()
            {
                Image = image,
                Name = name,
                
                Labels = new Dictionary<string, string> { { "vega-component", name } },
                ExposedPorts = ports?.ToDictionary(x => $"{x.ContainerPort}/tcp", y => default(EmptyStruct)),
                Volumes = volumes?.ToDictionary(x => x.Destination, y => new EmptyStruct()),
                HostConfig = new HostConfig()
                {
                    PortBindings = ports?.ToDictionary(x => $"{x.ContainerPort}/tcp", y => (IList<PortBinding>)new[] { new PortBinding() { HostPort = y.HostPort.ToString() } }.ToList()),
                    Binds = volumes?.Select(x => $"{x.Source}:{x.Destination}").ToArray(),
                    RestartPolicy = new RestartPolicy() { Name = RestartPolicyKind.UnlessStopped }
                }
            }).GetAwaiter().GetResult();

        return _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters() { }).GetAwaiter().GetResult();
    }

    public bool Exists(string name)
    {
        IList<ContainerListResponse> containers = _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>> { { "label", new Dictionary<string, bool> { { "vega-component", true } } } },
                Limit = 10,
            }).GetAwaiter().GetResult();
        var container = containers.FirstOrDefault(x => x.Labels["vega-component"].Contains(name));
        if (container == null) return false;

        return container.State == "running" ||
               // this should be started, lets do it
               _dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters() { }).GetAwaiter().GetResult();
    }

    public bool Delete(string name)
    {
        throw new NotImplementedException();
    }

    public bool Stop(string name)
    {
        throw new NotImplementedException();
    }

    public bool Exec(string name, string[] commands)
    {
        _dockerClient.Exec.ExecCreateContainerAsync(name, new ContainerExecCreateParameters
        {
            AttachStdout = true,
            AttachStderr = true,
            Cmd = commands,
        }).GetAwaiter().GetResult();
        return true;
    }
}
