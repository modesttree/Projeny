using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Specialized;

namespace Projeny
{
    public class ProjectConfig
    {
        public string ProjectSettingsPath;
        public List<string> AssetsFolder = new List<string>();
        public List<string> PluginsFolder = new List<string>();
        public List<string> SolutionProjects = new List<string>();
        public List<string> PackageFolders = new List<string>();
        public List<string> Prebuilt = new List<string>();
        public List<KeyValuePair<string, string>> SolutionFolders = new List<KeyValuePair<string, string>>();
    }
}

