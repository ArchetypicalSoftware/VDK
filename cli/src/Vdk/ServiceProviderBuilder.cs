using System.IO.Abstractions;
using Docker.DotNet;
using k8s;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.DependencyInjection;
using Vdk.Commands;
using Vdk.Data;
using Vdk.Services;
using System.Runtime.InteropServices;
using Vdk.Constants;

namespace Vdk;

public static class ServiceProviderBuilder
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();
        var yaml = new YamlObjectSerializer();

        _ = services
            .AddSingleton<Models.OperatingSystem>(s =>  
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return new(Platform.Linux);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return new(Platform.OSX);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return new(Platform.Windows);
                throw new NotSupportedException("Unknown OS");
            })
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
            .AddSingleton<CreateProxyCommand>()
            .AddSingleton<RemoveProxyCommand>()
            .AddSingleton<CreateRegistryCommand>()
            .AddSingleton<RemoveRegistryCommand>()
            .AddSingleton<IConsole, SystemConsole>()
            .AddSingleton<IFileSystem, FileSystem>()
            .AddSingleton<IShell, SystemShell>()
            .AddSingleton<IKindClient, KindClient>()
            .AddSingleton<IFluxClient, FluxClient>()
            .AddSingleton<IReverseProxyClient, ReverseProxyClient>()
            .AddSingleton<IEmbeddedDataReader, EmbeddedDataReader>()
            .AddSingleton<IDockerEngine, LocalDockerClient>()
            .AddSingleton<IHubClient,DockerHubClient>()
            .AddTransient<Func<string, IKubernetesClient>>(provider =>
            {
                return context =>
                {
                    var config = KubernetesClientConfiguration.LoadKubeConfig();
                    config.CurrentContext = $"kind-{context}";
                    return new KubernetesClient(KubernetesClientConfiguration.BuildConfigFromConfigObject(config));
                };
            })
            .AddSingleton<IDockerClient>(provider =>
            {
                var os = provider.GetRequiredService<Models.OperatingSystem>();

                return os.Platform switch
                {
                    Platform.Linux => new DockerClientConfiguration(
                        new Uri("unix:///var/run/docker.sock")).CreateClient(),
                    Platform.OSX => new DockerClientConfiguration(
                        new Uri("unix:///var/run/docker.sock")).CreateClient(),
                    Platform.Windows => new DockerClientConfiguration(
                        new Uri("npipe://./pipe/docker_engine")).CreateClient(),
                    _ => throw new NotSupportedException("Unknown OS")
                };
            });

        return services.BuildServiceProvider();
    }
}