using System.CommandLine;
using Vdk.Services;

namespace Vdk.Commands;

public class UpdateKindVersionInfoCommand: Command
{
    private readonly IKindVersionInfoService _client;

    public UpdateKindVersionInfoCommand(IKindVersionInfoService client) : base("kind-version-info",
        "Update kind version info (Maps kind and Kubernetes versions/enables new releases of kubernetes in vega)")
    {
        _client = client;
        this.SetHandler(InvokeAsync);
    }

    public Task InvokeAsync()
    {
        return _client.UpdateAsync();
    }
}