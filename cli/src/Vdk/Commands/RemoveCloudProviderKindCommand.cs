using System.CommandLine;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class RemoveCloudProviderKindCommand: Command
{
    private readonly IConsole _console;
    private readonly IHubClient _client;

    public RemoveCloudProviderKindCommand(IConsole console, IHubClient client) : base("cloud-provider-kind", "Remove Vega VDK Cloud Provider Kind")
    {
        _console = console;
        _client = client;
        this.SetHandler(InvokeAsync);
    }

    public Task InvokeAsync()
    {
        _client.DestroyCloudProviderKind();
        return Task.CompletedTask;
    }
}