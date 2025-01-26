using System.CommandLine;

namespace Vdk.Commands;

public class RemoveCommand: Command
{
    public RemoveCommand(RemoveClusterCommand removeCluster, RemoveRegistryCommand removeRegistry, RemoveProxyCommand removeProxy) 
        : base("remove", "Remove Vega development resources")
    {
        AddCommand(removeCluster);
        AddCommand(removeRegistry);
        AddCommand(removeProxy);
    }
}