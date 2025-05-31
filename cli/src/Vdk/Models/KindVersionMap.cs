namespace Vdk.Models;

public class KindVersionMap: List<KindVersion>
{
    public KindVersionMap()
    {
    }

    public KindVersionMap(IEnumerable<KindVersion> versions)
    {
        AddRange(versions);
    }

    public DateTime LastUpdated { get; set; } = DateTime.MinValue;

    public string? FindImage(string kindVersion, string kubeVersion)
    {
        var image = this.SingleOrDefault(k => k.Version == kindVersion)
                    ?.Images.OrderByDescending(i=>i.SemanticVersion)
                    .FirstOrDefault(i => i.Version.StartsWith($"{kubeVersion}."))
                    ?.Image;
        return image;
    }
}