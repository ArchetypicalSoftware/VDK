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
    public const string KubeApiVersion = "1.32";
    public const int ControlPlaneNodes = 1;
    public const int WorkerNodes = 2;

    public const string ConfigDirectoryName = "config";
    public const string KindVersionInfoFileName = "kind.version.info.json";
    public const int KindVersionInfoCacheMinutes = 1440;
}