using System.CommandLine;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class CreateCloudProviderKindCommand: Command
{
    private readonly IConsole _console;
    private readonly IHubClient _client;

    public CreateCloudProviderKindCommand(IConsole console, IHubClient client): base("cloud-provider-kind", "Create Vega VDK Cloud Provider kind container registry")
    {
        _console = console;
        _client = client;
        SetAction(_ => InvokeAsync());
    }

    public Task InvokeAsync()
    {
        _client.CreateCloudProviderKind();
        return Task.CompletedTask;
    }
}