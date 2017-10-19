using System.Collections.Generic;

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
        public List<string> ProjectPlatforms = new List<string>();
    }
}

