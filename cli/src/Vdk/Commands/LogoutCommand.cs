using System.CommandLine;
using Vdk.Services;

namespace Vdk.Commands;

public class LogoutCommand : Command
{
    private readonly IAuthService _auth;

    public LogoutCommand(IAuthService auth) : base("logout", "Remove local credentials for the current or specified profile")
    {
        _auth = auth;
        var profile = new Option<string?>("--profile") { Description = "Optional profile name to logout (defaults to current)" };
        Options.Add(profile);
        SetAction(async parseResult =>
        {
            var p = parseResult.GetValue(profile);
            if (!string.IsNullOrWhiteSpace(p))
            {
                _auth.SetCurrentProfile(p!);
            }
            await _auth.LogoutAsync(p);
        });
    }
}
