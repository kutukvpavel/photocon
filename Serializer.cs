using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace photocon;

public static class Serializer
{
    private static readonly ISerializer SerializerInstance = new SerializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
    private static readonly IDeserializer DeserializerInstance = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build(); 

    public static string Serialize<T>(T o)
    {
        return SerializerInstance.Serialize(o);
    }
    public static T Deserialize<T>(string yaml)
    {
        return DeserializerInstance.Deserialize<T>(yaml);
    }
}