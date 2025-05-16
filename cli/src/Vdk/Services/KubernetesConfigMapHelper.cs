using System.Collections.Generic;
using k8s.Models;

namespace Vdk.Services
{
    public static class KubernetesConfigMapHelper
    {
        public static V1ConfigMap CreateClusterInfoConfigMap(string tenantId, string env)
        {
            return new V1ConfigMap
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "cluster-info",
                    NamespaceProperty = "vega-system"
                },
                Data = new Dictionary<string, string>
                {
                    { "TenantId", tenantId },
                    { "Env", env }
                }
            };
        }
    }
}
