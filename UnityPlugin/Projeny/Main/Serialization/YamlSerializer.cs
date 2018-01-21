using System.Collections.Generic;
using System.IO;
using System.Text;
using PrjYamlDotNet.Core;
using PrjYamlDotNet.Serialization;
using PrjYamlDotNet.Serialization.NamingConventions;

namespace Projeny.Internal
{
    public static class YamlSerializer
    {
        const int IndentAmount = 6;

        public static string Serialize<T>(T config)
        {
            var serializer = new Serializer();
            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            var emitter = new Emitter(stringWriter, IndentAmount);
            serializer.Serialize(emitter, config);
            return stringBuilder.ToString();
        }

        public static T Deserialize<T>(string yamlStr)
        {
            var input = new StringReader(yamlStr);
            var deserializer = new Deserializer();
            return deserializer.Deserialize<T>(input);
        }
    }
}
