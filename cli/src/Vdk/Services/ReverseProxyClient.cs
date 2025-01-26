using k8s.Models;
using KubeOps.KubernetesClient;
using Vdk.Models;

namespace Vdk.Services;

internal class ReverseProxyClient : IReverseProxyClient
{
    private readonly IDockerEngine _docker;
    private readonly Func<string, IKubernetesClient> _client;
    //private const string NginxConf = "vega.conf";
    private static readonly string NginxConf = Path.Combine(".bin", "vega.conf");

    public ReverseProxyClient(IDockerEngine docker, Func<string, IKubernetesClient> client)
    {
        _docker = docker;
        _client = client;
        if (!_docker.Exists("nginx"))
        {
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
                    writer.WriteLine("    listen 443 ssl http2;");
                    writer.WriteLine("    listen [::]:443 ssl http2;");
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
            var fullChain = new FileInfo("Certs/fullchain.pem");
            var privKey = new FileInfo("Certs/privkey.pem");
            _docker.Run("nginx", "nginx",
                new[] { new PortMapping() { HostPort = 443, ContainerPort = 443 } },
                null,
                new[]
                {
                    new FileMapping() { Destination = "/etc/nginx/conf.d/vega.conf", Source = conf.FullName },
                    new FileMapping() { Destination = "/etc/certs/fullchain.pem", Source = fullChain.FullName },
                    new FileMapping() { Destination = "/etc/certs/privkey.pem", Source = privKey.FullName },
                },
                null);
        }
    }

    private const string StartComment = "##### START";
    private const string EndComment = "##### END";

    public void Upsert(string clusterName, int targetPortHttps, int targetPortHttp)
    {
        // create a new server block in the nginx conf pointing to the target port listening on the https://clusterName.dev-k8s.cloud domain
        // reload the nginx configuration

        

        using var writer = ClearCluster(clusterName);

        writer.WriteLine();
        writer.WriteLine($"##### START {clusterName}");
        writer.WriteLine("server {");
        writer.WriteLine($"    listen 443 ssl http2;");
        writer.WriteLine($"    listen [::]:443 ssl http2;");
        writer.WriteLine($"    server_name {clusterName}.dev-k8s.cloud;");
        writer.WriteLine($"    ssl_certificate  /etc/certs/fullchain.pem;");
        writer.WriteLine($"    ssl_certificate_key  /etc/certs/privkey.pem;");
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
        _client(clusterName).Create(tls);

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

    public void Delete(string clusterName)
    {
        ClearCluster(clusterName).Flush();
        ReloadConfigs();
    }

    public void List()
    {
        throw new NotImplementedException();
    }
}