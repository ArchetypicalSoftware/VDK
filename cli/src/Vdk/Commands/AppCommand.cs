using System.CommandLine;

namespace Vdk.Commands;

public class AppCommand: RootCommand
{
    public AppCommand(CreateCommand create) : base("Vega CLI - Manage Vega development environment")
    {
        AddCommand(create);
    }
}