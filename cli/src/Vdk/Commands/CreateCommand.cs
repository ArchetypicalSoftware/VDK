using System.CommandLine;

namespace Vdk.Commands;

public class CreateCommand: Command
{
    public CreateCommand(CreateClusterCommand createCluster) : base("create", "Create vega development resources")
    {
        AddCommand(createCluster);
    }
}