using Vdk.Models;

namespace Vdk.Services;

internal class ReverseProxyClient : IReverseProxyClient
{
    private readonly IDockerEngine _docker;
    private const string NginxConf = "vega.conf";

    public ReverseProxyClient(IDockerEngine docker)
    {
        _docker = docker;
        if (!_docker.Exists("nginx"))
        {
            var conf = new FileInfo(NginxConf);
            if (!conf.Exists)
            {
                using (var writer = conf.CreateText())
                {
                    writer.WriteLine("server {");
                    writer.WriteLine("    listen 443;");
                    writer.WriteLine("    listen [::]:443;");
                    writer.WriteLine("    server_name _;");
                    writer.WriteLine("    location / {");
                    writer.WriteLine("        proxy_pass http://localhost:80;");
                    writer.WriteLine("        proxy_set_header Host $host;");
                    writer.WriteLine("        proxy_set_header X-Real-IP $remote_addr;");
                    writer.WriteLine("        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;");
                    writer.WriteLine("        proxy_set_header X-Forwarded-Proto $scheme;");
                    writer.WriteLine("    }");
                    writer.WriteLine("}");
                }
            }

            _docker.Run("nginx", "nginx",
                new[] { new PortMapping() { HostPort = 443, ContainerPort = 443 } },
                null,
                new[] { new FileMapping() { Destination = "/etc/nginx/conf.d/vega.conf", Source = conf.FullName } },
                null);
        }
    }

    private const string StartComment = "##### START";
    private const string EndComment = "##### END";

    public void Upsert(string clusterName, int targetPortHttps, int targetPortHttp)
    {
        // create a new server block in the nginx conf pointing to the target port listening on the https://clusterName.dev-k8s.cloud domain
        // reload the nginx configuration

        var conf = new FileInfo(NginxConf);

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
        using var writer = conf.CreateText();
        foreach (var section in sections)
        {
            writer.WriteLine();
            foreach (var line in section.Value)
            {
                writer.WriteLine(line);
            }
            writer.WriteLine();
        }

        writer.WriteLine();
        writer.WriteLine($"##### START {clusterName}");
        writer.WriteLine("server {");
        writer.WriteLine($"    listen 443;");
        writer.WriteLine($"    listen [::]:443;");
        writer.WriteLine($"    server_name {clusterName}.dev-k8s.cloud;");
        writer.WriteLine("    location / {");
        writer.WriteLine($"       proxy_pass http://host.docker.internal:{targetPortHttps};");
        writer.WriteLine("        proxy_set_header Host $host;");
        writer.WriteLine("        proxy_set_header X-Real-IP $remote_addr;");
        writer.WriteLine("        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;");
        writer.WriteLine("        proxy_set_header X-Forwarded-Proto $scheme;");
        writer.WriteLine("    }");
        writer.WriteLine("}");
        writer.WriteLine($"##### END {clusterName}");
        writer.WriteLine();
        writer.Flush();

        _docker.Exec("nginx", new[] { "nginx", "-s", "reload" });
    }

    public void Delete(string clusterName)
    {
        throw new NotImplementedException();
    }

    public void List()
    {
        throw new NotImplementedException();
    }
}