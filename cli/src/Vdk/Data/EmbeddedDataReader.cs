using Vdk.Services;

namespace Vdk.Data;

public class EmbeddedDataReader : IEmbeddedDataReader
{
    private readonly IJsonObjectSerializer _serializer;

    public EmbeddedDataReader(IJsonObjectSerializer serializer)
    {
        _serializer = serializer;
    }

    public static IEmbeddedDataReader Default => new EmbeddedDataReader(new JsonObjectSerializer());

    public string Read(string path)
    {
        using (var stream = (typeof(EmbeddedDataReader).Assembly.GetManifestResourceStream(path)))
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