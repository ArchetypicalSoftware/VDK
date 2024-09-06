using System.CommandLine;

namespace Vdk.Commands;

public class AppCommand: RootCommand
{
    public AppCommand(CreateCommand create, RemoveCommand remove, ListCommand list, InitializeCommand init) : base("Vega CLI - Manage Vega development environment")
    {
        AddCommand(create);
        AddCommand(remove);
        AddCommand(list);
        AddCommand(init);
    }
}