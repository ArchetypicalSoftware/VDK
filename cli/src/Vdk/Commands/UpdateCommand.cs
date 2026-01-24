using System.CommandLine;

namespace Vdk.Commands;

public class UpdateCommand: Command
{
    public UpdateCommand(
        UpdateKindVersionInfoCommand updateKindVersionInfo,
        UpdateClustersCommand updateClusters) : base("update",
        "Update resources in vega development environment")
    {
        Subcommands.Add(updateKindVersionInfo);
        Subcommands.Add(updateClusters);
    }
}