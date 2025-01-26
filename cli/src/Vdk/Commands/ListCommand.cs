using System.CommandLine;

namespace Vdk.Commands;

public class ListCommand: Command
{
    public ListCommand(ListClustersCommand clustersCommand) : base("list", "List Vega development resources")
    {
        AddCommand(clustersCommand);
    }
}