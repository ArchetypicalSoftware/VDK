using System.CommandLine;

namespace Vdk.Commands;

public class UpdateCommand: Command
{
    public UpdateCommand(UpdateKindVersionInfoCommand updateKindVersionInfo) : base("update",
        "Update resources in vega development environment")
    {
        AddCommand(updateKindVersionInfo);
    }
}