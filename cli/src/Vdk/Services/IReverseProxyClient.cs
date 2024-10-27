using Docker.DotNet;
using Docker.DotNet.Models;
using Vdk.Models;

namespace Vdk.Services;

public interface IDockerEngine
{
    bool Run(string image, string name, PortMapping[]? ports, Dictionary<string, string>? envs, FileMapping[]? volumes, string[]? commands);

    bool Exists(string name);

    bool Delete(string name);

    bool Stop(string name);

    bool Exec(string name, string[] commands);
}

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

public interface IReverseProxyClient
{
    void Upsert(string clusterName, int targetPort);

    void Delete(string clusterName);

    void List();
}

internal class ReverseProxyClient : IReverseProxyClient
{
    private readonly IDockerEngine _docker;
    private const string NginxConf = "vega.conf";

    public ReverseProxyClient(IDockerEngine docker)
    {
        _docker = docker;
        if (!_docker.Exists("nginx"))
        {
            var conf = new FileInfo(NginxConf);
            if (!conf.Exists)
            {
                using (var writer = conf.CreateText())
                {
                    writer.WriteLine("server {");
                    writer.WriteLine("    listen 443;");
                    writer.WriteLine("    listen [::]:443;");
                    writer.WriteLine("    server_name _;");
                    writer.WriteLine("    location / {");
                    writer.WriteLine("        proxy_pass http://localhost:80;");
                    writer.WriteLine("        proxy_set_header Host $host;");
                    writer.WriteLine("        proxy_set_header X-Real-IP $remote_addr;");
                    writer.WriteLine("        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;");
                    writer.WriteLine("        proxy_set_header X-Forwarded-Proto $scheme;");
                    writer.WriteLine("    }");
                    writer.WriteLine("}");
                }
            }

            _docker.Run("nginx", "nginx",
                new[] { new PortMapping() { HostPort = 443, ContainerPort = 443 } },
                null,
                new[] { new FileMapping() { Destination = "/etc/nginx/conf.d/vega.conf", Source = conf.FullName } },
                null);
        }
    }

    private const string StartComment = "##### START";
    private const string EndComment = "##### END";

    public void Upsert(string clusterName, int targetPort)
    {
        // create a new server block in the nginx conf pointing to the target port listening on the https://clusterName.dev-k8s.cloud domain
        // reload the nginx configuration

        var conf = new FileInfo(NginxConf);

        // check all the server blocks and remove the one that matches the clusterName
        var lines = File.ReadAllLines(conf.FullName).ToList();
        // chunk all the lines into sections in a dictionary of string, list<string>, where the key is the cluster name and the value is the list of lines betweeh the START and END comments
        var sections = new Dictionary<string, List<string>>();
        var currentSection = new List<string>();
        var currentCluster = string.Empty;
        foreach (var line in lines)
        {
            if (line.Contains(StartComment))
            {
                if (currentSection.Any())
                {
                    sections.Add(currentCluster, currentSection);
                    currentSection = new List<string>();
                }
                currentCluster = line.Replace(StartComment, string.Empty).Trim();
            }
            currentSection.Add(line);
        }
        if (currentSection.Any())
        {
            sections.Add(currentCluster, currentSection);
        }
        if (sections.ContainsKey(clusterName))
        {
            sections.Remove(clusterName);
        }

        // clear the file and write the sections back
        using var writer = conf.CreateText();
        foreach (var section in sections)
        {
            writer.WriteLine();
            foreach (var line in section.Value)
            {
                writer.WriteLine(line);
            }
            writer.WriteLine();
        }

        writer.WriteLine();
        writer.WriteLine($"##### START {clusterName}");
        writer.WriteLine("server {");
        writer.WriteLine($"    listen 443;");
        writer.WriteLine($"    listen [::]:443;");
        writer.WriteLine($"    server_name {clusterName}.dev-k8s.cloud;");
        writer.WriteLine("    location / {");
        writer.WriteLine($"       proxy_pass http://host.docker.internal:{targetPort};");
        writer.WriteLine("        proxy_set_header Host $host;");
        writer.WriteLine("        proxy_set_header X-Real-IP $remote_addr;");
        writer.WriteLine("        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;");
        writer.WriteLine("        proxy_set_header X-Forwarded-Proto $scheme;");
        writer.WriteLine("    }");
        writer.WriteLine("}");
        writer.WriteLine($"##### END {clusterName}");
        writer.WriteLine();
        writer.Flush();

        _docker.Exec("nginx", new[] { "nginx", "-s", "reload" });
    }

    public void Delete(string clusterName)
    {
        throw new NotImplementedException();
    }

    public void List()
    {
        throw new NotImplementedException();
    }
}