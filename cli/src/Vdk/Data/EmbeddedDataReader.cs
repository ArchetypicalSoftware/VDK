using System.Reflection;
using Vdk.Services;

namespace Vdk.Data;

public class EmbeddedDataReader : IEmbeddedDataReader
{
    private readonly IJsonObjectSerializer _serializer;

    public EmbeddedDataReader(IJsonObjectSerializer serializer)
    {
        _serializer = serializer;
    }

    public EmbeddedDataReader(IJsonObjectSerializer serializer, Type refType)
    {
        _serializer = serializer;
        _refAssembly = refType.Assembly;
    }

    private readonly Assembly _refAssembly = typeof(EmbeddedDataReader).Assembly;

    public static IEmbeddedDataReader Default => new EmbeddedDataReader(new JsonObjectSerializer());

    public string Read(string path)
    {
        var names = _refAssembly.GetManifestResourceNames();
        var name = names.FirstOrDefault(x=>x.Equals(path, StringComparison.CurrentCultureIgnoreCase))??path;
        using (var stream = (_refAssembly.GetManifestResourceStream(name)))
        {
            if(stream == null) throw new FileNotFoundException();
            using (var reader = new StreamReader(stream))
            {
                var data = reader.ReadToEnd();
                return data;
            }
        }
    }

    public T ReadJsonObjects<T>(string path) where T: class, new()
    {
        return _serializer.Deserialize<T>(Read(path))??new();
    }
}