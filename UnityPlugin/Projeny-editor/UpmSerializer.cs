using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Projeny.Internal
{
    public static class UpmSerializer
    {
        public static string SerializeProjectConfig(ProjectConfig info)
        {
            return YamlSerializer.Serialize<ProjectConfigInternal>(ConvertToInternal(info));
        }

        public static ProjectConfig DeserializeProjectConfig(string yamlStr)
        {
            return ConvertToPublic(YamlSerializer.Deserialize<ProjectConfigInternal>(yamlStr));
        }

        public static string SerializeReleaseInfo(ReleaseInfo info)
        {
            return YamlSerializer.Serialize<ReleaseInfoInternal>(ConvertToInternal(info));
        }

        public static ReleaseInfo DeserializeReleaseInfo(string yamlStr)
        {
            return ConvertToPublic(YamlSerializer.Deserialize<ReleaseInfoInternal>(yamlStr));
        }

        public static string SerializePackageInfo(PackageInfo info)
        {
            return YamlSerializer.Serialize<PackageInfoInternal>(ConvertToInternal(info));
        }

        public static PackageInfo DeserializePackageInfo(string yamlStr)
        {
            return ConvertToPublic(YamlSerializer.Deserialize<PackageInfoInternal>(yamlStr));
        }

        static ProjectConfigInternal ConvertToInternal(ProjectConfig info)
        {
            return new ProjectConfigInternal()
            {
                Packages = info.Packages.ToList(),
                PackagesPlugins = info.PackagesPlugins.ToList(),
            };
        }

        static ReleaseInfoInternal ConvertToInternal(ReleaseInfo info)
        {
            return new ReleaseInfoInternal()
            {
                Title = info.Title,
                Version = info.Version,
            };
        }

        static PackageInfoInternal ConvertToInternal(PackageInfo info)
        {
            return new PackageInfoInternal()
            {
                Name = info.Name,
                Path = info.Path,
                Version = info.Version,
                InstallDate = info.InstallDate,
            };
        }

        static ProjectConfig ConvertToPublic(ProjectConfigInternal info)
        {
            if (info == null)
            {
                return null;
            }

            return new ProjectConfig()
            {
                Packages = info.Packages.ToList(),
                PackagesPlugins = info.PackagesPlugins.ToList(),
            };
        }

        static PackageInfo ConvertToPublic(PackageInfoInternal info)
        {
            if (info == null)
            {
                return null;
            }

            var newInfo = ScriptableObject.CreateInstance<PackageInfo>();

            newInfo.Name = info.Name;
            newInfo.Path = info.Path;
            newInfo.Version = info.Version;
            newInfo.InstallDate = info.InstallDate;

            return newInfo;
        }

        static ReleaseInfo ConvertToPublic(ReleaseInfoInternal info)
        {
            if (info == null)
            {
                return null;
            }

            var newInfo = ScriptableObject.CreateInstance<ReleaseInfo>();

            newInfo.Title = info.Title;
            newInfo.Version = info.Version;

            return newInfo;
        }

        // Yaml requires that we use properties, but Unity serialization requires
        // the opposite - that we use fields
        class ReleaseInfoInternal
        {
            public string Title
            {
                get;
                set;
            }

            public string Version
            {
                get;
                set;
            }
        }

        class ProjectConfigInternal
        {
            public List<string> Packages
            {
                get;
                set;
            }

            public List<string> PackagesPlugins
            {
                get;
                set;
            }
        }

        class PackageInfoInternal
        {
            public string Name
            {
                get;
                set;
            }

            public string Path
            {
                get;
                set;
            }

            public string Version
            {
                get;
                set;
            }

            public DateTime InstallDate
            {
                get;
                set;
            }
        }
    }
}
