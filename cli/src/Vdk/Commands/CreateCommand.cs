using System.CommandLine;

namespace Vdk.Commands;

public class CreateCommand: Command
{
    public CreateCommand(CreateClusterCommand createCluster, CreateRegistryCommand createRegistry, CreateProxyCommand createProxy) 
        : base("create", "Create vega development resources")
    {
        AddCommand(createCluster);
        AddCommand(createRegistry);
        AddCommand(createProxy);
    }
}