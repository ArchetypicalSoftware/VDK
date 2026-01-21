using System.CommandLine;
using Vdk.Services;

namespace Vdk.Commands;

public class LoginCommand : Command
{
    private readonly IAuthService _auth;

    public LoginCommand(IAuthService auth) : base("login", "Authenticate with the Vega identity provider using device code flow")
    {
        _auth = auth;
        var profile = new Option<string?>("--profile") { Description = "Optional profile name for this login (supports multiple accounts)" };
        Options.Add(profile);
        SetAction(async parseResult =>
        {
            var p = parseResult.GetValue(profile);
            if (!string.IsNullOrWhiteSpace(p))
            {
                _auth.SetCurrentProfile(p!);
            }
            await _auth.LoginAsync(p);
        });
    }
}
