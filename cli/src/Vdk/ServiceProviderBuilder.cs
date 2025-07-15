using System.IO.Abstractions;
using Docker.DotNet;
using k8s;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.DependencyInjection;
using Vdk.Commands;
using Vdk.Data;
using Vdk.Services;
using System.Runtime.InteropServices;
using System.Text;
using Octokit;
using Vdk.Constants;
using k8s.Exceptions;

namespace Vdk;

public static class ServiceProviderBuilder
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();
        var yaml = new YamlObjectSerializer();
        var gitHubClient = new GitHubClient(new ProductHeaderValue("vega-client"));
        services
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
            .AddSingleton<ListKubernetesVersions>()
            .AddSingleton<CreateProxyCommand>()
            .AddSingleton<RemoveProxyCommand>()
            .AddSingleton<CreateRegistryCommand>()
            .AddSingleton<CreateCloudProviderKindCommand>()
            .AddSingleton<RemoveRegistryCommand>()
            .AddSingleton<RemoveCloudProviderKindCommand>()
            .AddSingleton<UpdateCommand>()
            .AddSingleton<UpdateKindVersionInfoCommand>()
            .AddSingleton<IKindVersionInfoService, KindVersionInfoService>()
            .AddSingleton<IConsole, SystemConsole>()
            .AddSingleton<IFileSystem, FileSystem>()
            .AddSingleton<IShell, SystemShell>()
            .AddSingleton<IKindClient, KindClient>()
            .AddSingleton<IFluxClient, FluxClient>()
            .AddSingleton<IReverseProxyClient, ReverseProxyClient>()
            .AddSingleton<IEmbeddedDataReader, EmbeddedDataReader>()
            .AddSingleton<IDockerEngine>(provider =>
            {
                // Intelligent fallback logic
                var fileSystem = provider.GetRequiredService<IFileSystem>();
                var config = provider.GetRequiredService<GlobalConfiguration>();
                var fallbackFile = fileSystem.Path.Combine(config.VegaDirectory, ".vdk_docker_fallback");
                DateTime now = DateTime.UtcNow;
                DateTime fallbackUntil = DateTime.MinValue;

                if (fileSystem.File.Exists(fallbackFile))
                {
                    try
                    {
                        var content = fileSystem.File.ReadAllText(fallbackFile).Trim();
                        if (DateTime.TryParse(content, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsed))
                        {
                            fallbackUntil = parsed;
                        }
                    }
                    catch { /* ignore file errors */ }
                }

                if (fallbackUntil > now)
                {
                    // Still in fallback window
                    return new FallbackDockerEngine();
                }

                var docker = provider.GetService<IDockerClient>();
                if (docker == null) return new FallbackDockerEngine();

                var programatic = new LocalDockerClient(docker);
                if (!programatic.CanConnect())
                {
                    // Write fallback timestamp for 2 hours
                    try
                    {
                        fileSystem.File.WriteAllText(fallbackFile, now.AddHours(2).ToString("o"));
                    }
                    catch { /* ignore file errors */ }
                    return new FallbackDockerEngine();
                }

                return programatic;
            })
            .AddSingleton<IHubClient, DockerHubClient>()
            .AddSingleton<IGitHubClient>(gitHubClient)
            .AddSingleton<GlobalConfiguration>(new GlobalConfiguration())
            .AddSingleton<Func<string, IKubernetesClient>>(provider =>
            {
                // Static cache for kubeconfig YAMLs by context
                Dictionary<string, KubernetesClient> kubeConfigCache = new();
                object cacheLock = new();
                return context =>
                {
                    lock (cacheLock)
                    {
                        if (!kubeConfigCache.ContainsKey(context))
                        {
                            //Console.WriteLine($"Fetching kubeconfig for kind context: {context}");
                            var shell = provider.GetRequiredService<IShell>();
                            var kubeConfigYaml = shell.ExecuteAndCapture("kind", ["get", "kubeconfig", "-n", $"{context}"]);
                            using var stream = new MemoryStream(Encoding.Default.GetBytes(kubeConfigYaml));
                            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);
                            kubeConfigCache[context] = new KubernetesClient(config);
                        }
                        return kubeConfigCache[context];
                    }
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