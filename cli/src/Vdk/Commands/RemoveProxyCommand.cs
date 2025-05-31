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
        this.SetHandler(InvokeAsync);
    }

    public Task InvokeAsync()
    {
        try
        {
            _client.Delete();
        }
        catch (Exception ex)
        {
            _console.WriteError("Error removing proxy: {0}", ex.Message);
        }
        return Task.CompletedTask;
    }
}