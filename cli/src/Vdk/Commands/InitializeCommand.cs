using System.CommandLine;
using System.IO.Abstractions;
using Vdk.Constants;
using Vdk.Data;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class InitializeCommand : Command
{
    private readonly IConsole _console;
    private readonly IEmbeddedDataReader _dataReader;
    private readonly IYamlObjectSerializer _yaml;
    private readonly IFileSystem _fileSystem;
    private readonly IKindClient _kind;
    private readonly IFluxClient _flux;
    private readonly IReverseProxyClient _reverseProxy;

    public InitializeCommand(IConsole console, IEmbeddedDataReader dataReader, IYamlObjectSerializer yaml, IFileSystem fileSystem, IKindClient kind, IFluxClient flux, IReverseProxyClient reverseProxy)
        : base("init", "Initialize environment")
    {
        _console = console;
        _dataReader = dataReader;
        _yaml = yaml;
        _fileSystem = fileSystem;
        _kind = kind;
        _flux = flux;
        _reverseProxy = reverseProxy;
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

        await new CreateClusterCommand(_console, _dataReader, _yaml, _fileSystem, _kind, _flux, _reverseProxy)
            .InvokeAsync(name, controlPlaneNodes, workerNodes, kubeVersion);
    }
}