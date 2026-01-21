using System.CommandLine;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class CreateProxyCommand: Command
{
    private readonly IConsole _console;
    private readonly IReverseProxyClient _client;

    public CreateProxyCommand(IConsole console, IReverseProxyClient client) : base("proxy", "Create a Vega VDK Proxy Container")
    {
        _console = console;
        _client = client;
        SetAction(_ => InvokeAsync());
    }

    public Task InvokeAsync()
    {
        _client.Create();
        return Task.CompletedTask;
    }
}