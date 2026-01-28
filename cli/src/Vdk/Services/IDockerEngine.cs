using Vdk.Models;

namespace Vdk.Services;

public interface IDockerEngine
{
    bool Run(string image, string name, PortMapping[]? ports, Dictionary<string, string>? envs, FileMapping[]? volumes, string[]? commands, string? network = null);

    bool Exists(string name,bool checkRunning = true);

    bool Delete(string name);

    bool Stop(string name);

    bool Exec(string name, string[] commands);

    bool Restart(string name);

    bool CanConnect();
}