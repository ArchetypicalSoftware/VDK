using System.CommandLine;
using System.IO.Abstractions;
using Vdk.Constants;
using Vdk.Data;
using Vdk.Models;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class CreateClusterCommand: Command
{
    private readonly IConsole _console;
    private readonly IEmbeddedDataReader _dataReader;
    private readonly IYamlObjectSerializer _yaml;
    private readonly IFileSystem _fileSystem;
    private readonly IShell _shell;

    public CreateClusterCommand(IConsole console, IEmbeddedDataReader dataReader, IYamlObjectSerializer yaml, IFileSystem fileSystem, IShell shell) : base("cluster", "Create a vega development cluster")
    {
        _console = console;
        _dataReader = dataReader;
        _yaml = yaml;
        _fileSystem = fileSystem;
        _shell = shell;
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

    public async Task InvokeAsync(string name = Defaults.ClusterName, int controlPlaneNodes = 1, int workerNodes = 2, string kubeVersion = Defaults.KubeApiVersion)
    {
        var map = _dataReader.ReadJsonObjects<KindVersionMap>("Vdk.Data.KindVersionData.json");
        string? kindVersion = null;
        try
        {
            var kindVersionString = _shell.ExecuteAndCapture("kind", new[] { "--version" });
            kindVersion = (kindVersionString.Length > 13) ? kindVersionString.Substring(13).Trim() : null;
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
            cluster.Nodes.Add(node);
        }

        for (int index = 0; index < workerNodes; index++)
        {
            cluster.Nodes.Add(new KindNode() { Role = "worker", Image = image });
        }

        var manifest = _yaml.Serialize(cluster);
        var path = _fileSystem.Path.GetTempFileName();
        using (var writer = _fileSystem.File.CreateText(path))
        {
            _console.WriteLine(path);
            await writer.WriteAsync(manifest);
        }
        _shell.Execute("kind", new []{"create", "cluster", "--config", path, "--name", name.ToLower()});

        _shell.Execute("flux", new []{ "bootstrap", "github", "--owner=ArchetypicalSoftware", "--repository=vdk-flux", "--branch=initial", "--path=./clusters/default", "--private"});
    }
}