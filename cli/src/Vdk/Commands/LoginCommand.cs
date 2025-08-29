using System.CommandLine;
using Vdk.Services;

namespace Vdk.Commands;

public class LoginCommand : Command
{
    private readonly IAuthService _auth;

    public LoginCommand(IAuthService auth) : base("login", "Authenticate with the Vega identity provider using device code flow")
    {
        _auth = auth;
        var profile = new Option<string?>(new[] {"--profile"}, description: "Optional profile name for this login (supports multiple accounts)");
        AddOption(profile);
        this.SetHandler(async (string? p) =>
        {
            if (!string.IsNullOrWhiteSpace(p))
            {
                _auth.SetCurrentProfile(p!);
            }
            await _auth.LoginAsync(p);
        }, profile);
    }
}
