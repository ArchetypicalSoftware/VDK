using System.Net;
using System.Text.Json;
using k8s;
using k8s.Autorest;
using k8s.Models;
using KubeOps.KubernetesClient;
using Vdk.Models;

namespace Vdk.Services;

public class FluxClient : IFluxClient
{
    public const string DefaultBranch = "initial";
    
    private readonly Func<string, IKubernetesClient> _client;

    private readonly IConsole _console;
    private readonly IShell _shell;

    public FluxClient(IConsole console, IShell shell, Func<string, IKubernetesClient> client)
    {
        _console = console;
        _shell = shell;
        _client = client;
    }

    public void Bootstrap(string clusterName, string path, string branch = DefaultBranch)
    {
        _console.WriteLine("Flux bootstrapping. This may take a few minutes. Please wait...");
        try
        {
            // do not print stdout or stderr as they are not needed
            
            _shell.Execute("flux",["install"]);
            
        } catch (Exception ex)
        {
            _console.WriteLine($"Error installing Flux: {ex.Message}");
        }
        // Create a readonly git repo
        var repo = new GitRepo
        {
            ApiVersion = "source.toolkit.fluxcd.io/v1",
            Kind = "GitRepository",
            Metadata = new V1ObjectMeta()
            {
                Name = "vdk-flux",
                NamespaceProperty = "flux-system"
            },
            Spec = new GitRepoSpec()
            {
                Url = "https://github.com/ArchetypicalSoftware/vdk-flux.git",
                Interval = "2m0s",
                Ref = new GitRepoRef()
                {
                    Branch = branch
                }
            }
        };
        object? clusterRepo = null;
        try
        {
            clusterRepo = _client(clusterName).ApiClient.CustomObjects.GetNamespacedCustomObject("source.toolkit.fluxcd.io", "v1",
                "flux-system", "gitrepositories", repo.Name());
        }
        catch (HttpOperationException e)
        {
            if (e.Response.StatusCode != HttpStatusCode.NotFound)
                _console.WriteLine($"Git Repo creation failed: {e.Message}");
        }
        if (clusterRepo == null)
        {
            _client(clusterName).ApiClient.CustomObjects.CreateNamespacedCustomObject(repo, 
                "source.toolkit.fluxcd.io", "v1","flux-system",
                "gitrepositories");
        } //TODO: reconciliate and update ?
        
        var rootKustomization = new Kustomization()
        {
            ApiVersion = "kustomize.toolkit.fluxcd.io/v1",
            Kind = "Kustomization",
            Metadata = new V1ObjectMeta()
            {
                    Name = "vdk-flux",
                    NamespaceProperty = "flux-system"
            },
            Spec = new KustomizationSpec()
            {
                Path = "./clusters/default",
                Interval = "5m",
                Prune = true,
                SourceRef = new SourceRefKustomization()
                {
                    Kind = "GitRepository",
                    Name = "vdk-flux"
                }
            }
        };
        object? clusterRootKustomization = null;
        try
        {
            clusterRootKustomization = _client(clusterName).ApiClient.CustomObjects.GetNamespacedCustomObject("kustomize.toolkit.fluxcd.io", "v1",
                "flux-system", "kustomizations", rootKustomization.Name());
        }
        catch (HttpOperationException es)
        {
            if (es.Response.StatusCode != HttpStatusCode.NotFound)
                Console.WriteLine($"Root Kustomization creation failed: {es.Message}");
        }

        if (clusterRootKustomization == null)
        {
            _client(clusterName).ApiClient.CustomObjects.CreateNamespacedCustomObject(rootKustomization,
                "kustomize.toolkit.fluxcd.io", "v1", "flux-system", "kustomizations");
        }

        _console.WriteLine("Flux bootstrap complete.");

    }

    public bool WaitForKustomizations(string clusterName, int maxAttempts = 60, int delaySeconds = 5)
    {
        _console.WriteLine("Waiting for Flux kustomizations to reconcile...");

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var result = _client(clusterName).ApiClient.CustomObjects
                    .ListNamespacedCustomObject(
                        "kustomize.toolkit.fluxcd.io", "v1",
                        "flux-system", "kustomizations");

                var json = JsonSerializer.Serialize(result);
                using var doc = JsonDocument.Parse(json);
                var items = doc.RootElement.GetProperty("items");

                if (items.GetArrayLength() == 0)
                {
                    if (attempt % 5 == 0)
                        _console.WriteLine("  No kustomizations found yet. Waiting...");
                    Thread.Sleep(delaySeconds * 1000);
                    continue;
                }

                int total = items.GetArrayLength();
                int readyCount = 0;

                foreach (var item in items.EnumerateArray())
                {
                    var name = item.GetProperty("metadata").GetProperty("name").GetString();
                    bool isReady = false;

                    if (item.TryGetProperty("status", out var status) &&
                        status.TryGetProperty("conditions", out var conditions))
                    {
                        foreach (var condition in conditions.EnumerateArray())
                        {
                            if (condition.GetProperty("type").GetString() == "Ready")
                            {
                                var condStatus = condition.GetProperty("status").GetString();
                                if (condStatus == "True")
                                {
                                    isReady = true;
                                }
                                else if (attempt % 5 == 0)
                                {
                                    var reason = condition.TryGetProperty("reason", out var r)
                                        ? r.GetString() : "Unknown";
                                    var message = condition.TryGetProperty("message", out var m)
                                        ? m.GetString() : "";
                                    _console.WriteLine(
                                        $"  Kustomization '{name}' not ready: {reason} - {message}");
                                }
                                break;
                            }
                        }
                    }

                    if (isReady) readyCount++;
                }

                if (attempt % 5 == 0 || readyCount == total)
                    _console.WriteLine($"  Kustomizations ready: {readyCount}/{total}");

                if (readyCount == total)
                {
                    _console.WriteLine("All Flux kustomizations are ready.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (attempt % 5 == 0)
                    _console.WriteLine($"  Error checking kustomizations: {ex.Message}. Retrying...");
            }

            Thread.Sleep(delaySeconds * 1000);
        }

        _console.WriteWarning(
            "Timed out waiting for Flux kustomizations to reconcile. Proceeding anyway...");
        return false;
    }
}