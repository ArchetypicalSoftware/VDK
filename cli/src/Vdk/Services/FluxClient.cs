using System.Net;
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
}