using System.CommandLine;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class RemoveProxyCommand: Command
{
    private readonly IConsole _console;
    private readonly IReverseProxyClient _client;

    public RemoveProxyCommand(IConsole console, IReverseProxyClient client) : base("proxy", "Remove the Vega VDK Proxy")
    {
        _console = console;
        _client = client;
        SetAction(_ => InvokeAsync());
    }

    public Task InvokeAsync()
    {
        _client.Delete();
        return Task.CompletedTask;
    }
}