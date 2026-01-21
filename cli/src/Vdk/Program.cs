using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Vdk.Commands;

namespace Vdk;

class Program
{
    private static readonly IServiceProvider Services = ServiceProviderBuilder.Build();

    static async Task<int> Main(string[] args)
    {
        return await Services.GetRequiredService<AppCommand>().Parse(args).InvokeAsync();
    }
}
