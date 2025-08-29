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
    private readonly IAuthService _auth;

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
        GlobalConfiguration configs,
        IAuthService auth)
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
        _auth = auth;
        var nameOption = new Option<string>(new[] { "-n", "--Name" }, () => Defaults.ClusterName, "The name of the kind cluster to create.");
        var controlNodes = new Option<int>(new[] { "-c", "--ControlPlaneNodes" }, () => Defaults.ControlPlaneNodes, "The number of control plane nodes in the cluster.");
        var workers = new Option<int>(new[] { "-w", "--Workers" }, () => Defaults.WorkerNodes, "The number of worker nodes in the cluster.");
        var kubeVersion = new Option<string>(new[] { "-k", "--KubeVersion" }, () => "", "The kubernetes api version.");
        var labels = new Option<string>(new[] { "-l", "--Labels" }, () => "", "The labels to apply to the cluster to use in the configuration of Sectors. Each label pair should be separated by commas and the format should be KEY=VALUE. eg. KEY1=VAL1,KEY2=VAL2");
        AddOption(nameOption);
        AddOption(controlNodes);
        AddOption(workers);
        AddOption(kubeVersion);
        AddOption(labels);
        this.SetHandler(InvokeAsync, nameOption, controlNodes, workers, kubeVersion, labels);
    }

    public async Task InvokeAsync(string name = Defaults.ClusterName, int controlPlaneNodes = 1, int workerNodes = 2, string? kubeVersionRequested = null, string? labels = null)
    {
        // check if the hub and proxy are there
        if (!_reverseProxy.Exists())
            _reverseProxy.Create();
        if (!_hub.ExistRegistry())
            _hub.CreateRegistry();

        // validate the labels if they were passed in
        var pairs = (labels??"").Split(',').Select(x=>x.Split('='));
        if (pairs.Any())
        {
            //validate the labels and clean them up if needed
            foreach (var pair in pairs)
            {
                pair[0] = pair[0].Trim();
                if (pair.Length > 1)
                    pair[1] = pair[1].Trim();
                if (pair.Length != 2 || string.IsNullOrWhiteSpace(pair[0]) || string.IsNullOrWhiteSpace(pair[1]))
                {
                    _console.WriteError($"The provided label '{string.Join('=', pair)}' is not valid.  Labels must be in the format KEY=VALUE and multiple labels must be separated by commas.");
                    return;
                }
            }
        }

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
        var kubeVersion = string.IsNullOrWhiteSpace(kubeVersionRequested) ? await _kindVersionInfo.GetDefaultKubernetesVersionAsync(kindVersion) : kubeVersionRequested.Trim();
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
            var client = _clientFunc(name.ToLower());
            var ns = client.Get<V1Namespace>("vega-system");
            ns.EnsureMetadata().EnsureAnnotations()[_configs.MasterNodeAnnotation] = _yaml.Serialize(masterNode);
            client.Update(ns);

            // Write TenantId ConfigMap in vega-system
            var tenantId = await _auth.GetTenantIdAsync();
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                V1ConfigMap? cfg = null;
                try
                {
                    cfg = client.Get<V1ConfigMap>("vega-tenant", "vega-system");
                }
                catch { /* not found, will create */ }

                if (cfg is null)
                {
                    cfg = new V1ConfigMap(
                        metadata: new V1ObjectMeta(name: "vega-tenant", namespaceProperty: "vega-system"),
                        data: new Dictionary<string, string> { ["TenantId"] = tenantId }
                    );
                    client.Create(cfg);
                }
                else
                {
                    cfg.Data ??= new Dictionary<string, string>();
                    cfg.Data["TenantId"] = tenantId;
                    // add the label pairs here
                    foreach (var pair in pairs)
                    {
                        if (pair.Length == 2 && !string.IsNullOrWhiteSpace(pair[0]) && !string.IsNullOrWhiteSpace(pair[1]))
                        {
                            cfg.Data[$"{pair[0]}"] = pair[1];
                        }
                    }
                    client.Update(cfg);
                }
            }
            else
            {
                _console.WriteWarning("No TenantId found in token; skipping tenant config map.");
            }
        }
        catch (Exception e)
        {
            // print the stack trace
            _console.WriteLine(e.StackTrace);
            _console.WriteError("Failed to update reverse proxy or tenant config: " + e.Message);
            throw e;
        }
    }
}