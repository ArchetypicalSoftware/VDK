using System.CommandLine;

namespace Vdk.Commands;

public class ListCommand: Command
{
    public ListCommand(ListClustersCommand clustersCommand, ListKubernetesVersions kubernetesVersions) : base("list", "List Vega development resources")
    {
        Subcommands.Add(clustersCommand);
        Subcommands.Add(kubernetesVersions);
    }
}