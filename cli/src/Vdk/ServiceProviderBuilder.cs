using System.IO.Abstractions;
using Docker.DotNet;
using k8s;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.DependencyInjection;
using Vdk.Commands;
using Vdk.Data;
using Vdk.Services;

namespace Vdk;

public static class ServiceProviderBuilder
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();
        var yaml = new YamlObjectSerializer();

        services
            .AddSingleton<IYamlObjectSerializer>(yaml)
            .AddSingleton<IObjectSerializer>(yaml)
            .AddSingleton<IJsonObjectSerializer, JsonObjectSerializer>()
            .AddSingleton<AppCommand>()
            .AddSingleton<InitializeCommand>()
            .AddSingleton<CreateCommand>()
            .AddSingleton<RemoveCommand>()
            .AddSingleton<ListCommand>()
            .AddSingleton<CreateClusterCommand>()
            .AddSingleton<RemoveClusterCommand>()
            .AddSingleton<ListClustersCommand>()
            .AddSingleton<IConsole, SystemConsole>()
            .AddSingleton<IFileSystem, FileSystem>()
            .AddSingleton<IShell, SystemShell>()
            .AddSingleton<IKindClient, KindClient>()
            .AddSingleton<IFluxClient, FluxClient>()
            .AddSingleton<IReverseProxyClient, ReverseProxyClient>()
            .AddSingleton<IEmbeddedDataReader, EmbeddedDataReader>()
            .AddSingleton<IDockerEngine, LocalDockerClient>()
            .AddSingleton<IKubernetesClient>(provider =>
            {
                return new KubernetesClient(KubernetesClientConfiguration.BuildDefaultConfig());
            })
            .AddSingleton<IDockerClient>(provider =>
            {
                return new DockerClientConfiguration()
                    .CreateClient();
            });

        return services.BuildServiceProvider();
    }
}