using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Planar.Common
{
    public static class YmlUtil
    {
        public static T Deserialize<T>(string yml)
        {
            return YmlDeserializer.Deserialize<T>(yml);
        }

        public static string Serialize<T>(T item)
        {
            return YmlSerializer.Serialize(item);
        }

        private static IDeserializer YmlDeserializer
        {
            get
            {
                var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .IgnoreUnmatchedProperties()
                            .Build();
                return deserializer;
            }
        }

        private static ISerializer YmlSerializer
        {
            get
            {
                var serializer = new SerializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();
                return serializer;
            }
        }
    }
}