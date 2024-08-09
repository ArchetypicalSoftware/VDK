using System.CommandLine;

namespace Vdk.Commands;

public class RemoveCommand: Command
{
    public RemoveCommand(RemoveClusterCommand removeCluster) : base("remove", "Remove Vega development resources")
    {
        AddCommand(removeCluster);
    }
}