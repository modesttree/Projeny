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
}

