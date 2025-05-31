using KubeOps.KubernetesClient;
using System.CommandLine;
using System.IO.Abstractions;
using k8s.Models;
using Vdk.Constants;
using Vdk.Models;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class CreateClusterCommand : Command
{
    private readonly IConsole _console;
    private readonly IKindVersionInfoService _kindVersionInfo;
    private readonly IYamlObjectSerializer _yaml;
    private readonly IFileSystem _fileSystem;
    private readonly IKindClient _kind;
    private readonly IHubClient _hub;
    private readonly IFluxClient _flux;
    private readonly IReverseProxyClient _reverseProxy;
    private readonly Func<string, IKubernetesClient> _clientFunc;
    private readonly GlobalConfiguration _configs;

    public CreateClusterCommand(
        IConsole console,
        IKindVersionInfoService kindVersionInfo,
        IYamlObjectSerializer yaml,
        IFileSystem fileSystem,
        IKindClient kind,
        IHubClient hub,
        IFluxClient flux,
        IReverseProxyClient reverseProxy,
        Func<string, IKubernetesClient> clientFunc,
        GlobalConfiguration configs)
        : base("cluster", "Create a Vega development cluster")
    {
        _console = console;
        _kindVersionInfo = kindVersionInfo;
        _yaml = yaml;
        _fileSystem = fileSystem;
        _kind = kind;
        _hub = hub;
        _flux = flux;
        _reverseProxy = reverseProxy;
        _clientFunc = clientFunc;
        _configs = configs;
        var nameOption = new Option<string>(new[] { "-n", "--Name" }, () => Defaults.ClusterName, "The name of the kind cluster to create.");
        var controlNodes = new Option<int>(new[] { "-c", "--ControlPlaneNodes" }, () => Defaults.ControlPlaneNodes, "The number of control plane nodes in the cluster.");
        var workers = new Option<int>(new[] { "-w", "--Workers" }, () => Defaults.WorkerNodes, "The number of worker nodes in the cluster.");
        var kubeVersion = new Option<string>(new[] { "-k", "--KubeVersion" }, () => "1.29", "The kubernetes api version.");
        AddOption(nameOption);
        AddOption(controlNodes);
        AddOption(workers);
        AddOption(kubeVersion);
        this.SetHandler(InvokeAsync, nameOption, controlNodes, workers, kubeVersion);
    }

    public async Task InvokeAsync(string name = Defaults.ClusterName, int controlPlaneNodes = 1, int workerNodes = 2, string? kubeVersionRequested = null)
    {
        // check if the hub and proxy are there
        if (!_reverseProxy.Exists())
            _reverseProxy.Create();
        if (!_hub.Exists())
            _hub.Create();

        var map = await _kindVersionInfo.GetVersionInfoAsync();
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
        var kubeVersion = kubeVersionRequested ?? await _kindVersionInfo.GetDefaultKubernetesVersionAsync(kindVersion);
        var image = map.FindImage(kindVersion, kubeVersion);
        if (image is null)
        {
            // If image is null the most likely cause is that the user has a newly released version of kind and
            // we have not yet downloaded the latest version info.  To resolve this, we will attempt to circumvent
            // the cache timeout by directly calling UpdateAsync() and reloading the map.  If it still doesn't
            // find it, then we are truly in an error state.
            await _kindVersionInfo.UpdateAsync();
            map = await _kindVersionInfo.GetVersionInfoAsync();
            image = map.FindImage(kindVersion, kubeVersion);
        }
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
            cluster.Nodes.Add(new KindNode()
            {
                Role = "worker",
                Image = image,
                ExtraMounts = new List<Mount>
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
        var path = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), _fileSystem.Path.GetRandomFileName());
        await using (var writer = _fileSystem.File.CreateText(path))
        {
            // _console.WriteLine(path);
            await writer.WriteAsync(manifest);
        }

        // if the name is not provided, and the default cluster name is used.. iterate the clusters to find the next available name
        if (string.IsNullOrWhiteSpace(name) || name.ToLower() == Defaults.ClusterName)
        {
            var clusters = _kind.ListClusters();
            var i = 1;
            while (clusters.Any(x => x.name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                name = $"{Defaults.ClusterName}-{i}";
                i++;
            }
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
            var ns = _clientFunc(name.ToLower()).Get<V1Namespace>("vega-system");
            ns.EnsureMetadata().EnsureAnnotations()[_configs.MasterNodeAnnotation] = _yaml.Serialize(masterNode);
            _clientFunc(name.ToLower()).Update(ns);
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