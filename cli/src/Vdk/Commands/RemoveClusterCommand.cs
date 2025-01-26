using System.CommandLine;
using Vdk.Constants;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class RemoveClusterCommand : Command
{
    private readonly IConsole _console;
    private readonly IKindClient _kind;

    public RemoveClusterCommand(IConsole console, IKindClient kind) : base("cluster", "Remove a Vega development cluster")
    {
        _console = console;
        _kind = kind;

        var nameOption = new Option<string>(new[] { "-n", "--Name" }, () => Defaults.ClusterName, "The name of the cluster to remove");
        AddOption(nameOption);
        this.SetHandler(InvokeAsync, nameOption);
    }

    public Task InvokeAsync(string name = Defaults.ClusterName)
    {
        try
        {
            _kind.DeleteCluster(name);
        }
        catch (Exception ex)
        {
            _console.WriteError("Error removing cluster '{ClusterName}': {ErrorMessage}", name, ex.Message);
        }
        return Task.CompletedTask;
    }
}