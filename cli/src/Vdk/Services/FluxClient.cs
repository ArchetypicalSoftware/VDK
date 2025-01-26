namespace Vdk.Services;

public class FluxClient : IFluxClient
{
    public const string DefaultBranch = "initial";

    private readonly IConsole _console;
    private readonly IShell _shell;

    public FluxClient(IConsole console, IShell shell)
    {
        _console = console;
        _shell = shell;
    }

    public void Bootstrap(string path, string branch = DefaultBranch)
    {
        _shell.Execute("flux",
            [
                "bootstrap",
                "github",
                "--owner=ArchetypicalSoftware",
                "--repository=vdk-flux",
                $"--branch={branch}",
                $"--path={path}",
                "--read-write-key=false"
            ]);
    }
}