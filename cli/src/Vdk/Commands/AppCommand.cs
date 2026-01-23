using System.CommandLine;
using Vdk.Services;

namespace Vdk.Commands;

public class AppCommand : RootCommand
{
    public AppCommand(CreateCommand create, RemoveCommand remove, ListCommand list, InitializeCommand init, UpdateCommand update, LoginCommand login, LogoutCommand logout, IHubClient client) : base("Vega CLI - Manage Vega development environment")
    {
        Add(create);
        Add(remove);
        Add(list);
        Add(init);
        Add(update);
        Add(login);
        Add(logout);
    }
}