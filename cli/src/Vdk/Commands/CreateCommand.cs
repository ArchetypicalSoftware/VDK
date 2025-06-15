using System.CommandLine;

namespace Vdk.Commands;

public class CreateCommand: Command
{
    public CreateCommand(CreateClusterCommand createCluster, CreateRegistryCommand createRegistry, CreateProxyCommand createProxy, CreateCloudProviderKindCommand createCloudProviderKindCommand) 
        : base("create", "Create vega development resources")
    {
        AddCommand(createCluster);
        AddCommand(createRegistry);
        AddCommand(createProxy);
        AddCommand(createCloudProviderKindCommand);
    }
}