using System.CommandLine;
using Vdk.Services;

namespace Vdk.Commands;

public class AppCommand: RootCommand
{
    public AppCommand(CreateCommand create, RemoveCommand remove, ListCommand list, InitializeCommand init, IHubClient client) : base("Vega CLI - Manage Vega development environment")
    {
        AddCommand(create);
        AddCommand(remove);
        AddCommand(list);
        AddCommand(init);
    }
}