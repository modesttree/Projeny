using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Projeny
{
    public class ProjectConfig
    {
        public List<string> Packages
        {
            get;
            set;
        }

        public List<string> PluginPackages
        {
            get;
            set;
        }
    }

    public static class ProjectConfigSerializer
    {
        public static string Serialize(ProjectConfig config)
        {
            var serializer = new Serializer();
            var stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);
            serializer.Serialize(stringWriter, config);
            return stringBuilder.ToString();
        }

        public static ProjectConfig Deserialize(string yamlStr)
        {
            var input = new StringReader(yamlStr);
            var deserializer = new Deserializer();
            return deserializer.Deserialize<ProjectConfig>(input);
        }
    }
}
