namespace Vdk.Services;

public class KindClient : IKindClient
{
    private readonly IConsole _console;
    private readonly IShell _shell;

    public KindClient(IConsole console, IShell shell)
    {
        _console = console;
        _shell = shell;
    }

    public void CreateCluster(string name, string configPath)
    {
        _shell.Execute("kind", new[] { "create", "cluster", "--config", configPath, "--name", name.ToLower() });
    }

    public void DeleteCluster(string name)
    {
        _shell.Execute("kind", new [] {"delete", "cluster", "-n", name.ToLower()});
    }

    public List<string> ListClusters()
    {
        var response = _shell.ExecuteAndCapture("kind", new[] { "get", "clusters" });
        if (string.IsNullOrWhiteSpace(response)) return new List<string>();
        return response.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
            .ToList();
    }

    public string? GetVersion()
    {
        var response = GetVersionString();
        return (response.Length > 13) ? response.Substring(13).Trim() : null;
    }

    public string GetVersionString()
    {
        return _shell.ExecuteAndCapture("kind", new[] { "--version" });
    }
}