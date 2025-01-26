using System.CommandLine;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class ListClustersCommand: Command
{
    private readonly IConsole _console;
    private readonly IKindClient _client;

    public ListClustersCommand(IConsole console, IKindClient client) : base("clusters", "List Vega development clusters")
    {
        _console = console;
        _client = client;
        this.SetHandler(InvokeAsync);
    }

    public Task InvokeAsync()
    {
        var clusters = _client.ListClusters();
        clusters.ForEach(_console.WriteLine);
        return Task.CompletedTask;
    }
}