using System.Text.Json;
using Vdk.Constants;

namespace Vdk.Services;

public class JsonObjectSerializer: IJsonObjectSerializer
{
    public SerializerFormat Format => SerializerFormat.Json;

    public string? Serialize<T>(T? obj) where T : class
    {
        return (obj is null) ? null : JsonSerializer.Serialize(obj, Defaults.JsonOptions);
    }

    public T? Deserialize<T>(string? data) where T : class
    {
        return (data is null) ? null : JsonSerializer.Deserialize<T>(data, Defaults.JsonOptions);
    }
}