using k8s.Models;
using KubeOps.KubernetesClient;
using Vdk.Constants;
using Vdk.Models;

namespace Vdk.Services;

internal class ReverseProxyClient : IReverseProxyClient
{
    private readonly IDockerEngine _docker;
    private readonly Func<string, IKubernetesClient> _client;
    private readonly IConsole _console;

    //private const string NginxConf = "vega.conf";
    private static readonly string NginxConf = Path.Combine(".bin", "vega.conf");

    
    // ReverseProxyHostPort is 443 by default, unless REVERSE_PROXY_HOST_PORT is set as an env var
    private int ReverseProxyHostPort = GetEnvironmentVariableAsInt("REVERSE_PROXY_HOST_PORT", 443);
     

    public ReverseProxyClient(IDockerEngine docker, Func<string, IKubernetesClient> client, IConsole console)
    {
        _docker = docker;
        _client = client;
        _console = console;
    }

    private const string StartComment = "##### START";
    private const string EndComment = "##### END";

    public void Create()
    {
        bool proxyExists = false;
        try
        {
            proxyExists = _docker.Exists(Containers.ProxyName);
        }
        catch (Exception e)
        {
            _console.WriteWarning($"Failed to check if docker container {Containers.ProxyName} exists. Check configuration or try again.");
            _console.WriteError(e);
            return;
        }
        if (!proxyExists) {
            _console.WriteLine("Creating Vega VDK Proxy");
            _console.WriteLine(" - This may take a few minutes...");
            var conf = new FileInfo(NginxConf);
            if (!conf.Exists)
            {
                if (!conf.Directory!.Exists)
                {
                    conf.Directory.Create();
                }
                File.Create(conf.FullName).Dispose();
                using (var writer = conf.CreateText())
                {
                    writer.WriteLine("server {");
                    writer.WriteLine($"    listen {ReverseProxyHostPort} ssl;");
                    writer.WriteLine($"    listen [::]:{ReverseProxyHostPort} ssl;");
                    writer.WriteLine("    http2 on;");                    
                    writer.WriteLine("    server_name hub.dev-k8s.cloud;");
                    writer.WriteLine($"    ssl_certificate  /etc/certs/fullchain.pem;");
                    writer.WriteLine($"    ssl_certificate_key  /etc/certs/privkey.pem;");
                    writer.WriteLine("    location / {");
                    writer.WriteLine("        client_max_body_size 0;");
                    writer.WriteLine("        proxy_pass http://host.docker.internal:5000;");
                    writer.WriteLine("        proxy_set_header Host $host;");
                    writer.WriteLine("        proxy_set_header X-Real-IP $remote_addr;");
                    writer.WriteLine("        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;");
                    writer.WriteLine("        proxy_set_header X-Forwarded-Proto $scheme;");
                    writer.WriteLine("    }");
                    writer.WriteLine("}");
                }
            }
            else
            {
                _console.WriteWarning($" - Reverse proxy configuration for {conf.FullName} exists, skipping creation of file. Ensure it has the right values.");
            }
            var fullChain = new FileInfo("Certs/fullchain.pem");
            var privKey = new FileInfo("Certs/privkey.pem");
            try
            {
                _docker.Run(Containers.ProxyImage, Containers.ProxyName,
                    new[] { new PortMapping() { HostPort = ReverseProxyHostPort, ContainerPort = 443 } },
                    null,
                    new[]
                    {
                        new FileMapping() { Destination = "/etc/nginx/conf.d/vega.conf", Source = conf.FullName },
                        new FileMapping() { Destination = "/etc/certs/fullchain.pem", Source = fullChain.FullName },
                        new FileMapping() { Destination = "/etc/certs/privkey.pem", Source = privKey.FullName },
                    },
                    null);

            }
            catch (Exception e)
            {
                Console.WriteLine($"Error creating reverse proxy: {e}");
            }
        }
        else
        {
            _console.WriteLine("Vega VDK Proxy already created");
        }
    }

    public void Delete()
    {
        if (_docker.Exists(Containers.ProxyName, false))
        {
            _console.WriteWarning("Deleting Vega VDK Proxy from Docker");
            _console.WriteLine("You can recreate the proxy using command 'vega create proxy'");
            _docker.Delete(Containers.ProxyName);
        }
        else
        {
            _console.WriteLine("Proxy not found.");
        }
    }

    public void UpsertCluster(string clusterName, int targetPortHttps, int targetPortHttp)
    {
        // create a new server block in the nginx conf pointing to the target port listening on the https://clusterName.dev-k8s.cloud domain
        // reload the nginx configuration

        

        using var writer = ClearCluster(clusterName);

        writer.WriteLine();
        writer.WriteLine($"##### START {clusterName}");
        writer.WriteLine("server {");
        writer.WriteLine($"    listen {ReverseProxyHostPort} ssl;");
        writer.WriteLine($"    listen [::]:{ReverseProxyHostPort} ssl;");
        writer.WriteLine("    http2 on;");      
        writer.WriteLine($"    server_name {clusterName}.dev-k8s.cloud;");
        writer.WriteLine("    ssl_certificate  /etc/certs/fullchain.pem;");
        writer.WriteLine("    ssl_certificate_key  /etc/certs/privkey.pem;");
        writer.WriteLine("    location / {");
        writer.WriteLine($"        proxy_pass https://host.docker.internal:{targetPortHttps};");
        writer.WriteLine("        proxy_set_header Host $host;");
        writer.WriteLine("        proxy_set_header X-Real-IP $remote_addr;");
        writer.WriteLine("        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;");
        writer.WriteLine("        proxy_set_header X-Forwarded-Proto $scheme;");
        writer.WriteLine("    }");
        writer.WriteLine("}");
        writer.WriteLine($"##### END {clusterName}");
        writer.WriteLine();
        writer.Flush();

        // write the cert secret to the cluster
        if (_client(clusterName).Get<V1Namespace>("vega") == null)
        {
            var ns = new V1Namespace()
            {
                Metadata =
                {
                    Name = "vega"
                }
            };
            _client(clusterName).Create(ns);
        }

        var tls = new V1Secret()
        {
            Metadata = new()
            {
                Name = "dev-tls",
                NamespaceProperty = "vega"
            },
            Type = "kubernetes.io/tls",
            Data = new Dictionary<string, byte[]>
            {
                { "tls.crt", File.ReadAllBytes("Certs/fullchain.pem") },
                { "tls.key", File.ReadAllBytes("Certs/privkey.pem") }
            }
        };
        if (_client(clusterName).Get<V1Secret>("dev-tls", "vega") == null)
        {
            _console.WriteLine("Creating vega secret");
            _client(clusterName).Create(tls);    
        }

        ReloadConfigs();
    }

    private void ReloadConfigs()
    {
        _docker.Exec("nginx", new[] { "nginx", "-s", "reload" });
    }

    private static StreamWriter ClearCluster(string clusterName)
    {
        var conf = new FileInfo(NginxConf);
        StreamWriter? writer = null;
        try
        {
            // check all the server blocks and remove the one that matches the clusterName
            var lines = File.ReadAllLines(conf.FullName).ToList();
            // chunk all the lines into sections in a dictionary of string, list<string>, where the key is the cluster name and the value is the list of lines betweeh the START and END comments
            var sections = new Dictionary<string, List<string>>();
            var currentSection = new List<string>();
            var currentCluster = string.Empty;
            foreach (var line in lines)
            {
                if (line.Contains(StartComment))
                {
                    if (currentSection.Any())
                    {
                        sections.Add(currentCluster, currentSection);
                        currentSection = new List<string>();
                    }
                    currentCluster = line.Replace(StartComment, string.Empty).Trim();
                }
                currentSection.Add(line);
            }
            if (currentSection.Any())
            {
                sections.Add(currentCluster, currentSection);
            }
            if (sections.ContainsKey(clusterName))
            {
                sections.Remove(clusterName);
            }

            // clear the file and write the sections back
            writer = conf.CreateText();
            foreach (var section in sections)
            {
                writer.WriteLine();
                foreach (var line in section.Value)
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
            }

            return writer;
        }
        catch
        {
            writer?.Dispose();
            throw;
        }
    }

    public void DeleteCluster(string clusterName)
    {
        ClearCluster(clusterName).Flush();
        ReloadConfigs();
    }

    public void List()
    {
        throw new NotImplementedException();
    }
    
    private static int GetEnvironmentVariableAsInt(string variableName, int defaultValue = 0)
    {
        string strValue = Environment.GetEnvironmentVariable(variableName);
        if (strValue != null && int.TryParse(strValue, out int intValue))
        {
            return intValue;
        }
        return defaultValue;
    }
}