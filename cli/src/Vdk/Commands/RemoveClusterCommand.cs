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

        var nameOption = new Option<string>("--Name") { DefaultValueFactory = _ => Defaults.ClusterName, Description = "The name of the cluster to remove" };
        nameOption.Aliases.Add("-n");
        Options.Add(nameOption);
        SetAction(parseResult => InvokeAsync(parseResult.GetValue(nameOption) ?? Defaults.ClusterName));
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