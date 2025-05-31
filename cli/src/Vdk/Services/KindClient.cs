using k8s.Models;
using KubeOps.KubernetesClient;
using Vdk.Models;

namespace Vdk.Services;

public class KindClient : IKindClient
{
    private readonly IConsole _console;
    private readonly IShell _shell;
    private readonly Func<string, IKubernetesClient> _client;
    private readonly IYamlObjectSerializer _yaml;
    private readonly GlobalConfiguration _configs;

    public KindClient(IConsole console, IShell shell, Func<string, IKubernetesClient> client, IYamlObjectSerializer yaml, GlobalConfiguration configs)
    {
        _console = console;
        _shell = shell;
        _client = client;
        _yaml = yaml;
        _configs = configs;
    }

    public void CreateCluster(string name, string configPath)
    {
        _shell.Execute("kind", ["create", "cluster", "--config", configPath, "--name", name.ToLower()]);
    }

    public void DeleteCluster(string name)
    {
        _shell.Execute("kind", ["delete", "cluster", "-n", name.ToLower()]);
    }

    public List<(string name, bool isVdk, KindNode? master)> ListClusters()
    {
        var response = _shell.ExecuteAndCapture("kind", ["get", "clusters"]);
        if (string.IsNullOrWhiteSpace(response)) return new List<(string name, bool isVdk, KindNode? master)>();
        // Split by new lines and trim each line, removing empty entries
        var names = response.Split(Environment.NewLine.ToArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
            .ToList();

        // get a client for each one and check to see if its a vdk cluster
        var vdkClusters = new List<(string name, bool isVdk, KindNode? master)>();
        foreach (var name in names)
        {
            try
            {
                var k8SClient = _client(name);
                // check the cluster for the vega-system namespace. If it has it, then go fetch the mapped ports
                var ns = k8SClient.Get<V1Namespace>("vega-system");
                if (ns != null && ns.EnsureMetadata().EnsureAnnotations().ContainsKey(_configs.MasterNodeAnnotation))
                {
                    try
                    {
                        var node = _yaml.Deserialize<KindNode>(ns.Annotations()[_configs.MasterNodeAnnotation]);
                        vdkClusters.Add(node != null ? (name, true, node) : (name, false, null));
                    }
                    catch (Exception)
                    {
                        // couldnt parse
                    }
                }
                else
                    vdkClusters.Add((name, false, null));
            }
            catch (Exception ex)
            {
                _console.WriteLine($"Error checking cluster {name}: {ex.Message}");
            }
        }

        return vdkClusters;
    }

    public string? GetVersion()
    {
        var response = GetVersionString();
        return (response.Length > 13) ? response.Substring(13).Trim() : null;
    }

    public string GetVersionString()
    {
        return _shell.ExecuteAndCapture("kind", ["--version"]);
    }
}