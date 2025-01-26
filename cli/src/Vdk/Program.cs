using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Vdk.Commands;

namespace Vdk;

class Program
{
    private static readonly IServiceProvider Services = ServiceProviderBuilder.Build();

    static async Task Main(string[] args)
    {
        await Services.GetRequiredService<AppCommand>().InvokeAsync(args);
    }
}
