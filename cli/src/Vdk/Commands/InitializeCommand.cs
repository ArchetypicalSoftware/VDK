using System.CommandLine;
using Vdk.Constants;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class InitializeCommand : Command
{
    private readonly CreateClusterCommand _createCluster;
    private readonly CreateProxyCommand _createProxy;
    private readonly CreateRegistryCommand _createRegistry;
    private readonly IKindClient _kind;
    private readonly IConsole _console;

    public InitializeCommand(CreateClusterCommand createCluster, CreateProxyCommand createProxy, CreateRegistryCommand createRegistry, IKindClient kind, IConsole console)
        : base("init", "Initialize environment")
    {
        _createCluster = createCluster;
        _createProxy = createProxy;
        _createRegistry = createRegistry;
        _kind = kind;
        _console = console;
        this.SetHandler(InvokeAsync);
    }

    public async Task InvokeAsync()
    {
        var name = Defaults.ClusterName;
        var controlPlaneNodes = 1;
        var workerNodes = 2;
        var kubeVersion = Defaults.KubeApiVersion;

        var existing = _kind.ListClusters();
        if (existing.Any(x => x.Equals(name, StringComparison.CurrentCultureIgnoreCase)))
        {
            return;
        }

        _console.WriteLine("Welcome to Vega! Initializing your environment");
        await _createProxy.InvokeAsync();
        await _createRegistry.InvokeAsync();
        await _createCluster.InvokeAsync(name, controlPlaneNodes, workerNodes, kubeVersion);
    }
}