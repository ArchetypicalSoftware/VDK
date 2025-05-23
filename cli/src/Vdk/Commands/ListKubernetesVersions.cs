using System.CommandLine;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class ListKubernetesVersions: Command
{
    private readonly IConsole _console;
    private readonly IKindClient _client;
    private readonly IKindVersionInfoService _versionInfo;

    public ListKubernetesVersions(IConsole console, IKindClient client, IKindVersionInfoService versionInfo) 
        : base("kubernetes-versions", "List available kubernetes versions")
    {
        _console = console;
        _client = client;
        _versionInfo = versionInfo;
        this.SetHandler(InvokeAsync);
    }

    public async Task InvokeAsync()
    {
        var kindVersion = _client.GetVersion();
        var map = await _versionInfo.GetVersionInfoAsync();
        var current =
            map.SingleOrDefault(x => x.Version.Equals(kindVersion, StringComparison.CurrentCultureIgnoreCase));
        if (current is not null)
        {
            foreach (var image in current.Images.OrderByDescending(x=>x.SemanticVersion))
            {
                _console.WriteLine(image.Version);
            }
        }
        else
        {
            _console.WriteWarning("No kubernetes versions found for kind version {version}", kindVersion ?? "[None]");
        }
    }
}