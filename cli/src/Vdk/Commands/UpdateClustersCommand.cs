using System.CommandLine;
using System.IO.Abstractions;
using k8s.Models;
using KubeOps.KubernetesClient;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class UpdateClustersCommand : Command
{
    private readonly IConsole _console;
    private readonly IKindClient _kind;
    private readonly IFileSystem _fileSystem;
    private readonly Func<string, IKubernetesClient> _clientFunc;
    private readonly IReverseProxyClient _reverseProxy;

    public UpdateClustersCommand(
        IConsole console,
        IKindClient kind,
        IFileSystem fileSystem,
        Func<string, IKubernetesClient> clientFunc,
        IReverseProxyClient reverseProxy)
        : base("clusters", "Update cluster configurations (certificates, etc.)")
    {
        _console = console;
        _kind = kind;
        _fileSystem = fileSystem;
        _clientFunc = clientFunc;
        _reverseProxy = reverseProxy;

        var verboseOption = new Option<bool>("--verbose") { Description = "Enable verbose output for debugging" };
        verboseOption.Aliases.Add("-v");

        Options.Add(verboseOption);
        SetAction(parseResult => InvokeAsync(parseResult.GetValue(verboseOption)));
    }

    public async Task InvokeAsync(bool verbose = false)
    {
        // Load local certificates
        var fullChainPath = _fileSystem.Path.Combine("Certs", "fullchain.pem");
        var privKeyPath = _fileSystem.Path.Combine("Certs", "privkey.pem");

        // Check for and fix certificate paths that were incorrectly created as directories
        // This can happen on Mac when Docker creates directories for missing mount paths
        FixCertificatePathIfDirectory(fullChainPath, verbose);
        FixCertificatePathIfDirectory(privKeyPath, verbose);

        if (!_fileSystem.File.Exists(fullChainPath) || !_fileSystem.File.Exists(privKeyPath))
        {
            _console.WriteError("Certificate files not found. Expected: Certs/fullchain.pem and Certs/privkey.pem");
            return;
        }

        var localCert = await _fileSystem.File.ReadAllBytesAsync(fullChainPath);
        var localKey = await _fileSystem.File.ReadAllBytesAsync(privKeyPath);

        if (verbose)
        {
            _console.WriteLine($"[DEBUG] Local certificate size: {localCert.Length} bytes");
            _console.WriteLine($"[DEBUG] Local private key size: {localKey.Length} bytes");
        }

        // Get all VDK clusters
        var clusters = _kind.ListClusters();
        var vdkClusters = clusters.Where(c => c.isVdk).ToList();

        if (vdkClusters.Count == 0)
        {
            _console.WriteWarning("No VDK clusters found.");
            return;
        }

        _console.WriteLine($"Found {vdkClusters.Count} VDK cluster(s) to check.");

        foreach (var cluster in vdkClusters)
        {
            await UpdateClusterCertificates(cluster.name, localCert, localKey, verbose);
        }

        _console.WriteLine("Cluster certificate update complete.");

        // Regenerate nginx configuration for all clusters (adds WebSocket support, etc.)
        _console.WriteLine();
        if (_reverseProxy.Exists())
        {
            _reverseProxy.RegenerateConfigs();
        }
        else
        {
            if (verbose)
            {
                _console.WriteLine("[DEBUG] Reverse proxy not running, skipping nginx config regeneration.");
            }
        }
    }

    private async Task UpdateClusterCertificates(string clusterName, byte[] localCert, byte[] localKey, bool verbose)
    {
        _console.WriteLine($"Checking cluster: {clusterName}");

        IKubernetesClient client;
        try
        {
            client = _clientFunc(clusterName);
        }
        catch (Exception ex)
        {
            _console.WriteError($"Failed to connect to cluster '{clusterName}': {ex.Message}");
            if (verbose)
            {
                _console.WriteLine($"[DEBUG] Exception: {ex}");
            }
            return;
        }

        // Get all namespaces
        IList<V1Namespace> namespaces;
        try
        {
            namespaces = client.List<V1Namespace>();
        }
        catch (Exception ex)
        {
            _console.WriteError($"Failed to list namespaces in cluster '{clusterName}': {ex.Message}");
            if (verbose)
            {
                _console.WriteLine($"[DEBUG] Exception: {ex}");
            }
            return;
        }

        if (verbose)
        {
            _console.WriteLine($"[DEBUG] Found {namespaces.Count} namespace(s) in cluster '{clusterName}'");
        }

        var updatedSecrets = new List<(string Namespace, string SecretName)>();

        foreach (var ns in namespaces)
        {
            var nsName = ns.Metadata?.Name;
            if (string.IsNullOrEmpty(nsName)) continue;

            if (verbose)
            {
                _console.WriteLine($"[DEBUG] Scanning namespace: {nsName}");
            }

            await UpdateNamespaceCertificates(client, clusterName, nsName, localCert, localKey, verbose, updatedSecrets);
        }

        // Restart gateways if any secrets were updated
        if (updatedSecrets.Count > 0)
        {
            _console.WriteLine($"  Updated {updatedSecrets.Count} secret(s). Restarting affected gateways...");
            await RestartGateways(client, clusterName, updatedSecrets, verbose);
        }
        else
        {
            _console.WriteLine($"  All certificates are up to date.");
        }
    }

    private async Task UpdateNamespaceCertificates(
        IKubernetesClient client,
        string clusterName,
        string nsName,
        byte[] localCert,
        byte[] localKey,
        bool verbose,
        List<(string Namespace, string SecretName)> updatedSecrets)
    {
        IList<V1Secret> secrets;
        try
        {
            secrets = client.List<V1Secret>(nsName);
        }
        catch (Exception ex)
        {
            if (verbose)
            {
                _console.WriteLine($"[DEBUG] Failed to list secrets in namespace '{nsName}': {ex.Message}");
            }
            return;
        }

        // Filter to TLS secrets only
        var tlsSecrets = secrets.Where(s => s.Type == "kubernetes.io/tls").ToList();

        if (verbose && tlsSecrets.Count > 0)
        {
            _console.WriteLine($"[DEBUG] Found {tlsSecrets.Count} TLS secret(s) in namespace '{nsName}'");
        }

        foreach (var secret in tlsSecrets)
        {
            var secretName = secret.Metadata?.Name;
            if (string.IsNullOrEmpty(secretName)) continue;

            // Only update secrets that are managed by Vega:
            // 1. Named "dev-tls" (the standard Vega TLS secret)
            // 2. Have the annotation "vega.dev/managed=true"
            var annotations = secret.Metadata?.Annotations;
            bool isVegaManaged = secretName == "dev-tls" ||
                                 (annotations != null && annotations.TryGetValue("vega.dev/managed", out var managed) && managed == "true");

            if (!isVegaManaged)
            {
                if (verbose)
                {
                    _console.WriteLine($"[DEBUG] Secret '{nsName}/{secretName}' is not Vega-managed, skipping.");
                }
                continue;
            }

            if (secret.Data == null)
            {
                if (verbose)
                {
                    _console.WriteLine($"[DEBUG] Secret '{nsName}/{secretName}' has no data, skipping.");
                }
                continue;
            }

            // Get current cert and key from secret
            secret.Data.TryGetValue("tls.crt", out var currentCert);
            secret.Data.TryGetValue("tls.key", out var currentKey);

            if (currentCert == null || currentKey == null)
            {
                if (verbose)
                {
                    _console.WriteLine($"[DEBUG] Secret '{nsName}/{secretName}' is missing tls.crt or tls.key, skipping.");
                }
                continue;
            }

            // Compare certificates
            bool certNeedsUpdate = !currentCert.SequenceEqual(localCert);
            bool keyNeedsUpdate = !currentKey.SequenceEqual(localKey);

            if (verbose)
            {
                _console.WriteLine($"[DEBUG] Secret '{nsName}/{secretName}': cert match={!certNeedsUpdate}, key match={!keyNeedsUpdate}");
            }

            if (certNeedsUpdate || keyNeedsUpdate)
            {
                _console.WriteLine($"  Updating secret: {nsName}/{secretName}");

                try
                {
                    secret.Data["tls.crt"] = localCert;
                    secret.Data["tls.key"] = localKey;
                    client.Update(secret);
                    updatedSecrets.Add((nsName, secretName));

                    if (verbose)
                    {
                        _console.WriteLine($"[DEBUG] Successfully updated secret '{nsName}/{secretName}'");
                    }
                }
                catch (Exception ex)
                {
                    _console.WriteError($"Failed to update secret '{nsName}/{secretName}': {ex.Message}");
                    if (verbose)
                    {
                        _console.WriteLine($"[DEBUG] Exception: {ex}");
                    }
                }
            }
        }

        await Task.CompletedTask;
    }

    private async Task RestartGateways(
        IKubernetesClient client,
        string clusterName,
        List<(string Namespace, string SecretName)> updatedSecrets,
        bool verbose)
    {
        // Group by namespace for efficiency
        var namespaces = updatedSecrets.Select(s => s.Namespace).Distinct();

        foreach (var nsName in namespaces)
        {
            var secretsInNs = updatedSecrets.Where(s => s.Namespace == nsName).Select(s => s.SecretName).ToHashSet();

            // Try to find Gateway resources (gateway.networking.k8s.io)
            await RestartGatewayApiGateways(client, nsName, secretsInNs, verbose);

            // Also check for Ingress resources that might reference the secrets
            await RestartIngressControllers(client, nsName, secretsInNs, verbose);
        }
    }

    private async Task RestartGatewayApiGateways(
        IKubernetesClient client,
        string nsName,
        HashSet<string> secretNames,
        bool verbose)
    {
        try
        {
            // Get deployments in the namespace that might be gateway controllers
            var deployments = client.List<V1Deployment>(nsName);

            foreach (var deployment in deployments)
            {
                var deploymentName = deployment.Metadata?.Name ?? "";

                // Look for gateway-related deployments
                if (deploymentName.Contains("gateway", StringComparison.OrdinalIgnoreCase) ||
                    deploymentName.Contains("envoy", StringComparison.OrdinalIgnoreCase) ||
                    deploymentName.Contains("ingress", StringComparison.OrdinalIgnoreCase))
                {
                    if (verbose)
                    {
                        _console.WriteLine($"[DEBUG] Found potential gateway deployment: {nsName}/{deploymentName}");
                    }

                    // Trigger a rollout restart by updating an annotation
                    await RolloutRestartDeployment(client, deployment, verbose);
                }
            }
        }
        catch (Exception ex)
        {
            if (verbose)
            {
                _console.WriteLine($"[DEBUG] Failed to check Gateway API gateways in namespace '{nsName}': {ex.Message}");
            }
        }
    }

    private async Task RestartIngressControllers(
        IKubernetesClient client,
        string nsName,
        HashSet<string> secretNames,
        bool verbose)
    {
        try
        {
            // Check if any ingresses reference the updated secrets
            var ingresses = client.List<V1Ingress>(nsName);

            foreach (var ingress in ingresses)
            {
                var ingressName = ingress.Metadata?.Name ?? "";
                var tls = ingress.Spec?.Tls;

                if (tls == null) continue;

                foreach (var tlsEntry in tls)
                {
                    if (!string.IsNullOrEmpty(tlsEntry.SecretName) && secretNames.Contains(tlsEntry.SecretName))
                    {
                        _console.WriteLine($"  Ingress '{nsName}/{ingressName}' references updated secret '{tlsEntry.SecretName}'");

                        if (verbose)
                        {
                            _console.WriteLine($"[DEBUG] Ingress controller should automatically pick up the new certificate");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (verbose)
            {
                _console.WriteLine($"[DEBUG] Failed to check ingresses in namespace '{nsName}': {ex.Message}");
            }
        }

        await Task.CompletedTask;
    }

    private async Task RolloutRestartDeployment(IKubernetesClient client, V1Deployment deployment, bool verbose)
    {
        var deploymentName = deployment.Metadata?.Name;
        var nsName = deployment.Metadata?.NamespaceProperty;

        if (string.IsNullOrEmpty(deploymentName) || string.IsNullOrEmpty(nsName))
            return;

        try
        {
            // Add/update restart annotation to trigger rollout
            deployment.Spec ??= new V1DeploymentSpec();
            deployment.Spec.Template ??= new V1PodTemplateSpec();
            deployment.Spec.Template.Metadata ??= new V1ObjectMeta();
            deployment.Spec.Template.Metadata.Annotations ??= new Dictionary<string, string>();

            var restartTime = DateTime.UtcNow.ToString("o");
            deployment.Spec.Template.Metadata.Annotations["vega.dev/restartedAt"] = restartTime;

            client.Update(deployment);
            _console.WriteLine($"  Restarted deployment: {nsName}/{deploymentName}");

            if (verbose)
            {
                _console.WriteLine($"[DEBUG] Set restart annotation to '{restartTime}'");
            }
        }
        catch (Exception ex)
        {
            _console.WriteWarning($"Failed to restart deployment '{nsName}/{deploymentName}': {ex.Message}");
            if (verbose)
            {
                _console.WriteLine($"[DEBUG] Exception: {ex}");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Checks if a certificate path exists as a directory instead of a file
    /// and removes it. On some systems (especially Mac), Docker may incorrectly
    /// create directories when mounting paths that don't exist.
    /// </summary>
    private void FixCertificatePathIfDirectory(string path, bool verbose)
    {
        if (_fileSystem.Directory.Exists(path))
        {
            _console.WriteWarning($"Certificate path '{path}' exists as a directory instead of a file. Removing...");
            try
            {
                _fileSystem.Directory.Delete(path, recursive: true);
                if (verbose)
                {
                    _console.WriteLine($"[DEBUG] Successfully removed directory '{path}'");
                }
            }
            catch (Exception ex)
            {
                _console.WriteError($"Failed to remove directory '{path}': {ex.Message}");
            }
        }
    }
}
