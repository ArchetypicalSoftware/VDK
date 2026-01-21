using System.CommandLine;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class CreateRegistryCommand: Command
{
    private readonly IConsole _console;
    private readonly IHubClient _client;

    public CreateRegistryCommand(IConsole console, IHubClient client): base("registry", "Create Vega VDK Container Registry")
    {
        _console = console;
        _client = client;
        SetAction(_ => InvokeAsync());
    }

    public Task InvokeAsync()
    {
        _client.CreateRegistry();
        return Task.CompletedTask;
    }
}