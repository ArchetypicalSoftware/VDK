using Vdk.Constants;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Vdk.Services;

public class YamlObjectSerializer : IYamlObjectSerializer
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public YamlObjectSerializer()
    {
        _serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).IgnoreFields().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults).Build();
        _deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).IgnoreUnmatchedProperties().Build();
    }

    public YamlObjectSerializer(ISerializer serializer, IDeserializer deserializer)
    {
        _serializer = serializer;
        _deserializer = deserializer;
    }

    public SerializerFormat Format => SerializerFormat.Yaml;

    public string? Serialize<T>(T? obj) where T: class
    {
        return obj is null ? null : _serializer.Serialize(obj);
    }

    public T? Deserialize<T>(string? data) where T : class
    {
        return data is null ? null : _deserializer.Deserialize<T>(data);
    }
    
}