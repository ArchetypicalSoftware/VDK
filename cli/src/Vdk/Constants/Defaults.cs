using System.Text.Json;

namespace Vdk.Constants;

public static class Defaults
{
    public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        IncludeFields = false, 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, 
        PropertyNameCaseInsensitive = true
    };

    public const string ClusterName = "vdk";
    public const string KubeApiVersion = "1.29";
    public const int ControlPlaneNodes = 1;
    public const int WorkerNodes = 2;
}