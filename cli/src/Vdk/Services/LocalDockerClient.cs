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
        // Validate and fix volume mount sources before creating container
        // This prevents Docker from creating directories when files are expected
        if (volumes != null)
        {
            foreach (var volume in volumes)
            {
                EnsureVolumeMountSource(volume.Source, volume.Destination);
            }
        }

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
                Filters = new Dictionary<string, IDictionary<string, bool>> { { "name", new Dictionary<string, bool> { { name, true } } } },
                All = true,
            }).GetAwaiter().GetResult();
        var container = containers.FirstOrDefault(x => x.Names.Any(n => n.TrimStart('/') == name));

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
                Filters = new Dictionary<string, IDictionary<string, bool>> { { "name", new Dictionary<string, bool> { { name, true } } } },
                All = true,
            }).GetAwaiter().GetResult();
        var container = containers.FirstOrDefault(x => x.Names.Any(n => n.TrimStart('/') == name));
        if (container != null)
        {
            _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters(){Force = true}).GetAwaiter().GetResult();
        }
        return true;
    }

    public bool Stop(string name)
    {
        throw new NotImplementedException();
    }

    public bool Exec(string name, string[] commands)
    {
        var container = _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>> { { "name", new Dictionary<string, bool> { { name, true } } } },
            }).GetAwaiter().GetResult().FirstOrDefault(x => x.Names.Any(n => n.TrimStart('/') == name));

        if (container == null) return false;
        _dockerClient.Exec.ExecCreateContainerAsync(container.ID, new ContainerExecCreateParameters
        {
            AttachStdout = true,
            AttachStderr = true,
            Cmd = commands,
        }).GetAwaiter().GetResult();
        return true;
    }

    public bool Restart(string name)
    {
        var container = _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                Filters = new Dictionary<string, IDictionary<string, bool>> { { "name", new Dictionary<string, bool> { { name, true } } } },
                All = true,
            }).GetAwaiter().GetResult().FirstOrDefault(x => x.Names.Any(n => n.TrimStart('/') == name));

        if (container == null) return false;
        _dockerClient.Containers.RestartContainerAsync(container.ID, new ContainerRestartParameters()).GetAwaiter().GetResult();
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

    /// <summary>
    /// Ensures that a volume mount source path exists correctly.
    /// Fixes the common Docker issue where mounting a non-existent file creates a directory instead.
    /// </summary>
    private void EnsureVolumeMountSource(string sourcePath, string destinationPath)
    {
        // Determine if destination looks like a file (has extension) or directory
        bool isFilePath = Path.HasExtension(destinationPath) && !destinationPath.EndsWith('/') && !destinationPath.EndsWith('\\');

        // Check if path was incorrectly created as a directory when it should be a file
        if (isFilePath && Directory.Exists(sourcePath))
        {
            Console.WriteLine($"Certificate path '{sourcePath}' exists as a directory instead of a file. Removing...");
            try
            {
                Directory.Delete(sourcePath, recursive: true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to remove directory '{sourcePath}': {ex.Message}",
                    ex);
            }
        }

        // Ensure parent directory exists
        var parentDir = Path.GetDirectoryName(sourcePath);
        if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        // For file paths, ensure the file exists
        if (isFilePath && !File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                $"Mount source file does not exist: '{sourcePath}'. " +
                $"Please ensure the required config file exists before running this command.",
                sourcePath);
        }

        // For directory paths, ensure directory exists
        if (!isFilePath && !Directory.Exists(sourcePath))
        {
            Directory.CreateDirectory(sourcePath);
        }
    }
}