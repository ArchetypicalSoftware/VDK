using k8s;
using k8s.Models;

namespace Vdk.Models;

public class GitRepo : IKubernetesObject<V1ObjectMeta>
{
    public string ApiVersion { get; set; }
    public string Kind { get; set; }
    public V1ObjectMeta Metadata { get; set; }
    public GitRepoSpec Spec { get; set; }
}

public class GitRepoSpec
{
    public string Url { get; set; }

    public string Interval { get; set; }
    
    public GitRepoRef Ref { get; set; }
    
}
public class GitRepoRef
{
    public string Branch { get; set; }
}
