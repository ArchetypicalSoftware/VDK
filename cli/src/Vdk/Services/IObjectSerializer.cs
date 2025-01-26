using Vdk.Constants;

namespace Vdk.Services;

public interface IObjectSerializer
{
    SerializerFormat Format { get; }
    string? Serialize<T>(T? obj) where T: class;
    T? Deserialize<T>(string? data) where T : class;
}