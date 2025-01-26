using Vdk.Constants;

namespace Vdk.Models;

public class OperatingSystem
{
    public OperatingSystem(Platform platform)
    {
        Platform = platform;
    }

    public Platform Platform { get; set; }
}