using System.CommandLine;
using System.IO.Abstractions;
using Vdk.Constants;
using Vdk.Data;
using Vdk.Models;
using Vdk.Services;
using k8s.Models;
using System.Diagnostics;
using KubeOps.KubernetesClient;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class CreateClusterCommand : Command
{
    private readonly IConsole _console;
    private readonly IEmbeddedDataReader _dataReader;
    private readonly IYamlObjectSerializer _yaml;
    private readonly IFileSystem _fileSystem;
    private readonly IKindClient _kind;
    private readonly IFluxClient _flux;
    private readonly IReverseProxyClient _reverseProxy;
    private readonly Func<string, IKubernetesClient> _kubeClient;

    public CreateClusterCommand(IConsole console, IEmbeddedDataReader dataReader, IYamlObjectSerializer yaml, IFileSystem fileSystem, IKindClient kind, IFluxClient flux, IReverseProxyClient reverseProxy, Func<string, IKubernetesClient> kubeClient)
        : base("cluster", "Create a Vega development cluster")
    {
        _console = console;
        _dataReader = dataReader;
        _yaml = yaml;
        _fileSystem = fileSystem;
        _kind = kind;
        _flux = flux;
        _reverseProxy = reverseProxy;
        _kubeClient = kubeClient;
        var nameOption = new Option<string>(new[] { "-n", "--Name" }, () => Defaults.ClusterName, "The name of the kind cluster to create.");
        var controlNodes = new Option<int>(new[] { "-c", "--ControlPlaneNodes" }, () => Defaults.ControlPlaneNodes, "The number of control plane nodes in the cluster.");
        var workers = new Option<int>(new[] { "-w", "--Workers" }, () => Defaults.WorkerNodes, "The number of worker nodes in the cluster.");
        var kubeVersion = new Option<string>(new[] { "-k", "--KubeVersion" }, () => "1.29", "The kubernetes api version.");
        AddOption(nameOption);
        AddOption(controlNodes);
        AddOption(workers);
        AddOption(kubeVersion);
        this.SetHandler((string name, int controlPlaneNodes, int workerNodes, string kubeVersion, bool bypassPrompt) => InvokeAsync(name, controlPlaneNodes, workerNodes, kubeVersion, bypassPrompt),
            nameOption, controlNodes, workers, kubeVersion, new Option<bool>("--bypassPrompt", () => false, "Bypass the tenant prompt (for tests)"));
    }

    public async Task InvokeAsync(string name = Defaults.ClusterName, int controlPlaneNodes = 1, int workerNodes = 2, string kubeVersion = Defaults.KubeApiVersion, bool bypassPrompt = false)
    {
        // Ensure config exists and prompt if not
        var config = ConfigManager.EnsureConfig(
            promptTenantId: () => {
                _console.Write("Tenant GUID: ");
                return Console.ReadLine();
            },
            openBrowser: () => {
                var url = "https://archetypical.software/register";
                try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); } catch { }
            },
            bypassPrompt: bypassPrompt);

        var map = _dataReader.ReadJsonObjects<KindVersionMap>("Vdk.Data.KindVersionData.json");
        string? kindVersion = null;
        try
        {
            kindVersion = _kind.GetVersion();
        }
        catch (Exception ex)
        {
            _console.WriteError($"Unable to retrieve the installed kind version. {ex.Message}");
            return;
        }

        if (string.IsNullOrWhiteSpace(kindVersion))
        {
            _console.WriteWarning($"Kind version {kindVersion} is not supported by the current VDK.");
            return;
        }
        var image = map.FindImage(kindVersion, kubeVersion);
        if (image == null)
        {
            if (map.FirstOrDefault(m => m.Version == kindVersion) is null)
            {
                _console.WriteWarning($"Kind version '{kindVersion}' is not supported by the VDK.");
            }
            _console.WriteLine($"Unable to find image for kind version '{kindVersion}' and kubernetes api version '{kubeVersion}'");
            return;
        }

        var cluster = new KindCluster();
        for (int index = 0; index < controlPlaneNodes; index++)
        {
            var node = new KindNode { Role = "control-plane", Image = image };
            if (index == 0)
            {
                node.Labels = new Dictionary<string, string>
                {
                    {"ingress-ready", "true"}
                };
                node.ExtraPortMappings = PortMapping.Defaults;
            }
            node.ExtraMounts = new List<Mount>
            {
                new()
                {
                    HostPath = _fileSystem.FileInfo.New("ConfigMounts/hosts.toml").FullName, 
                    ContainerPath = "/etc/containerd/certs.d/hub.dev-k8s.cloud/hosts.toml"
                }
            };
            cluster.Nodes.Add(node);
        }

        for (int index = 0; index < workerNodes; index++)
        {
            cluster.Nodes.Add(new KindNode() { Role = "worker", Image = image, ExtraMounts = new List<Mount>
                {
                    new()
                    {
                        HostPath = _fileSystem.FileInfo.New("ConfigMounts/hosts.toml").FullName,
                        ContainerPath = "/etc/containerd/certs.d/hub.dev-k8s.cloud/hosts.toml"
                    }
                }
            });
        }

        // add the containerd config patches
        cluster.ContainerdConfigPatches =
        [ kindVersion == "0.27.0" ? 
         @"
[plugins.""io.containerd.cri.v1.images"".registry]
  config_path = ""/etc/containerd/certs.d""
".Trim()
        :
            @"
[plugins.""io.containerd.grpc.v1.cri"".registry]
  config_path = ""/etc/containerd/certs.d""
".Trim()
        ];

        var manifest = _yaml.Serialize(cluster);
        var path = _fileSystem.Path.GetTempFileName();
        await using (var writer = _fileSystem.File.CreateText(path))
        {
            // _console.WriteLine(path);
            await writer.WriteAsync(manifest);
        }

        _kind.CreateCluster(name.ToLower(), path);
        var masterNode = cluster.Nodes.FirstOrDefault(x => x.ExtraPortMappings?.Any() == true);
        if (masterNode == null)
        {
            _console.WriteError("Unable to find the master node");
            return;
        }

        _flux.Bootstrap(name.ToLower(), "./clusters/default", branch: "main");

        
        try
        {
            _reverseProxy.UpsertCluster(name.ToLower(), masterNode.ExtraPortMappings.First().HostPort,
                masterNode.ExtraPortMappings.Last().HostPort);
        }
        catch (Exception e)
        {
            // print the stack trace
            _console.WriteLine(e.StackTrace);
            _console.WriteError("Failed to update reverse proxy: " + e.Message);
            throw e;
        }
        
    }
}