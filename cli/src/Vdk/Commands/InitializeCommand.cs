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
    private readonly IKindVersionInfoService _kindVersionInfo;

    public InitializeCommand(CreateClusterCommand createCluster, CreateProxyCommand createProxy, CreateRegistryCommand createRegistry,
        IKindClient kind, IConsole console, IKindVersionInfoService kindVersionInfo)
        : base("init", "Initialize environment")
    {
        _createCluster = createCluster;
        _createProxy = createProxy;
        _createRegistry = createRegistry;
        _kind = kind;
        _console = console;
        _kindVersionInfo = kindVersionInfo;
        this.SetHandler(InvokeAsync);
    }

    public async Task InvokeAsync()
    {
        var name = Defaults.ClusterName;
        var controlPlaneNodes = 1;
        var workerNodes = 2;
        var kindVersion = _kind.GetVersion();
        if (kindVersion is null)
        {
            _console.WriteWarning("Unable to detect kind version.  Please ensure kind is installed in your environment");
            return;
        }
        var kubeVersion = await _kindVersionInfo.GetDefaultKubernetesVersionAsync(kindVersion);

        var existing = _kind.ListClusters().Select(x => x.name);
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