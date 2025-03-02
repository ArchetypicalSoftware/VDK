using k8s;
using k8s.Models;

namespace Vdk.Models;

public class Kustomization : IKubernetesObject<V1ObjectMeta>
{
    public string ApiVersion { get; set; }
    public string Kind { get; set; }
    public V1ObjectMeta Metadata { get; set; }
    public KustomizationSpec Spec { get; set; }
    
}

public class KustomizationSpec
{
    public string Interval { get; set; }
    public string Path { get; set; }
    public bool Prune { get; set; }
    public SourceRefKustomization SourceRef { get; set; }
}

public class SourceRefKustomization
{
    public string Kind { get; set; }
    public string Name { get; set; }
}