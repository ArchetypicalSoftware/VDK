using System.CommandLine;

namespace Vdk.Commands;

public class CreateCommand: Command
{
    public CreateCommand(CreateClusterCommand createCluster, CreateRegistryCommand createRegistry, CreateProxyCommand createProxy, CreateCloudProviderKindCommand createCloudProviderKindCommand)
        : base("create", "Create vega development resources")
    {
        Subcommands.Add(createCluster);
        Subcommands.Add(createRegistry);
        Subcommands.Add(createProxy);
        Subcommands.Add(createCloudProviderKindCommand);
    }
}