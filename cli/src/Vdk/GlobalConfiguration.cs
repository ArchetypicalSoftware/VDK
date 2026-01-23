using Vdk.Constants;

namespace Vdk;

public class GlobalConfiguration
{
    internal string? _profileDirectory = null;

    public string ConfigDirectoryName { get; set; } = Defaults.ConfigDirectoryName;

    public string VegaDirectory
    {
        get
        {
            return _profileDirectory ??=
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vega");
        }
    }

    public string ConfigDirectoryPath => Path.Combine(VegaDirectory, ConfigDirectoryName);
    public string KindVersionInfoFilePath => Path.Combine(ConfigDirectoryPath, Defaults.KindVersionInfoFileName);

    public string MasterNodeAnnotation = "vdk.vega.io/cluster";

    // OAuth / Hydra configuration (defaults can be overridden later)
    public string HydraDeviceAuthorizationEndpoint { get; set; } = "https://idp.dev-k8s.cloud/oidc/oauth2/device/auth";
    public string HydraTokenEndpoint { get; set; } = "https://idp.dev-k8s.cloud/oidc/oauth2/token";
    public string OAuthClientId { get; set; } = "vega-cli";
    public string[] OAuthScopes { get; set; } = new[] { "openid", "offline", "profile" };

    // JWT claim names
    public string TenantIdClaim { get; set; } = "tenant_id";
}