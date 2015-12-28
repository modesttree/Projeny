using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SharpYaml;
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
            var yamlStr = serializer.Serialize(config);

            var result = new StringBuilder();

            foreach (var line in yamlStr.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                if (line.StartsWith("!"))
                {
                    // Ignore the lines that include explicit type information
                    continue;
                }

                if (line.EndsWith(": []"))
                {
                    // Just don't output empty lists
                    continue;
                }

                result.AppendLine(line);
            }

            return result.ToString();
        }

        public static ProjectConfig Deserialize(string yamlStr)
        {
            var deserializer = new Serializer();
            return (ProjectConfig)deserializer.Deserialize(yamlStr, typeof(ProjectConfig), null);
        }
    }
}
