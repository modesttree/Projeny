using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Projeny
{
    public static class YamlSerializer
    {
        public static string Serialize<T>(T config)
        {
            var serializer = new Serializer(namingConvention: NamingConvention);
            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            serializer.Serialize(stringWriter, config);
            return stringBuilder.ToString();
        }

        public static T Deserialize<T>(string yamlStr)
        {
            var input = new StringReader(yamlStr);
            var deserializer = new Deserializer(
                namingConvention: NamingConvention);
            return deserializer.Deserialize<T>(input);
        }

        static INamingConvention NamingConvention
        {
            get
            {
                return new CamelCaseNamingConvention();
            }
        }
    }
}
