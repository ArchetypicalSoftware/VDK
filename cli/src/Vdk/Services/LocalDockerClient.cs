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

    public bool Run(string image, string name, PortMapping[]? ports, Dictionary<string, string>? envs, FileMapping[]? volumes, string[]? commands, string? network = null)
    {
        _dockerClient.Images.CreateImageAsync(
          new ImagesCreateParameters
          {
              FromImage = image,
          },
          null,
          new Progress<JSONMessage>()).GetAwaiter().GetResult();
        var extraHosts = Environment.OSVersion.Platform == PlatformID.Unix ? (List<string>)["host.docker.internal:host-gateway"] : null;
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
                    RestartPolicy = new RestartPolicy() { Name = RestartPolicyKind.UnlessStopped },
                    // ExtraHosts should be a list of strings where each string is in the format "<host>:<port>"
                    // This may not be required for mac/windows but it wont work for linux without it.
                    // can we detect the architecture and set it accordingly?
                    ExtraHosts = extraHosts
                    
                }
            }).GetAwaiter().GetResult();
        // Connect to custom Docker network if specified
        if (!string.IsNullOrWhiteSpace(network))
        {
            var networkList = _dockerClient.Networks
                .ListNetworksAsync(new NetworksListParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        { "name", new Dictionary<string, bool> { { network, true } } }
                    }
                })
                .GetAwaiter()
                .GetResult();

            var dockerNetwork = networkList?.FirstOrDefault();
            if (dockerNetwork == null)
            {
                throw new Exception($"Docker network '{network}' not found.");
            }

            _dockerClient.Networks
                .ConnectNetworkAsync(dockerNetwork.ID, new NetworkConnectParameters
                {
                    Container = response.ID
                })
                .GetAwaiter()
                .GetResult();
        }

        return _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters() { }).GetAwaiter().GetResult();
    }

    public bool Exists(string name, bool checkRunning = true)
    {
        IList<ContainerListResponse> containers = _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>> { { "label", new Dictionary<string, bool> { { "vega-component", true } } } },
                Limit = 10,
            }).GetAwaiter().GetResult();
        var container = containers.FirstOrDefault(x => x.Labels["vega-component"].Contains(name));
        
        if (container == null) return false;
        if (checkRunning == false) return true;
        
        return container.State == "running" ||
               // this should be started, lets do it
               _dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters() { }).GetAwaiter().GetResult();
    }

    public bool Delete(string name)
    {
        IList<ContainerListResponse> containers = _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>> { { "label", new Dictionary<string, bool> { { "vega-component", true } } } },
                Limit = 10,
            }).GetAwaiter().GetResult();
        var container = containers.FirstOrDefault(x => x.Labels["vega-component"].Contains(name));
        if (container != null)
        {
            _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters(){Force = true});
        }
        return true;
    }

    public bool Stop(string name)
    {
        throw new NotImplementedException();
    }

    public bool Exec(string name, string[] commands)
    {
        // Get container id from name and labels
        var container = _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>> { { "label", new Dictionary<string, bool> { { "vega-component", true } } } },
                Limit = 10,
            }).GetAwaiter().GetResult().FirstOrDefault(x => x.Labels["vega-component"].Contains(name));

        if (container == null) return false;
        _dockerClient.Exec.ExecCreateContainerAsync(container.ID, new ContainerExecCreateParameters
        {
            AttachStdout = true,
            AttachStderr = true,
            Cmd = commands,
        }).GetAwaiter().GetResult();
        return true;
    }

    public bool CanConnect()
    {
        try
        {
            _dockerClient.Images.ListImagesAsync(new ImagesListParameters()).GetAwaiter().GetResult();
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in CanConnect: {ex.Message}");
            return false;
        }
    }
}