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
}