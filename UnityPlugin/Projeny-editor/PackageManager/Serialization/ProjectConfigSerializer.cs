using System.Collections.Generic;
using System.IO;
using System.Text;
using SharpYaml.Serialization;
using Projeny.Internal;

namespace Projeny
{
    public class ProjectConfig
    {
        public List<string> Packages = new List<string>();
        public List<string> PluginPackages = new List<string>();
    }

    public static class ProjectConfigSerializer
    {
        public static string Serialize(ProjectConfig config)
        {
            var serializer = new Serializer();
            return serializer.Serialize(config);
        }

        public static ProjectConfig Deserialize(string yamlStr)
        {
            var deserializer = new Serializer();
            return (ProjectConfig)deserializer.Deserialize(yamlStr, typeof(ProjectConfig), null);
        }
    }
}
