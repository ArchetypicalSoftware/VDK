using System.CommandLine;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Vdk.Commands;
using Vdk.Services;

namespace Vdk;

class Program
{
    private static readonly IServiceProvider Services = ServiceProviderBuilder.Build();

    static async Task<int> Main(string[] args)
    {
        var auth = Services.GetRequiredService<IAuthService>();
        // Skip auth for explicit non-exec commands
        var skipAuth = args.Length == 0 ||
                       args.Contains("--help") || args.Contains("-h") ||
                       args.Contains("--version") ||
                       (args.Length > 0 && (string.Equals(args[0], "login", StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(args[0], "logout", StringComparison.OrdinalIgnoreCase)));
        if (!skipAuth)
        {
            await auth.EnsureAuthenticatedAsync();
        }
        return await Services.GetRequiredService<AppCommand>().Parse(args).InvokeAsync();
    }
}
