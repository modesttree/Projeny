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

            newInfo.Name = info.Name;

            newInfo.HasVersionCode = info.VersionCode.HasValue;

            if (info.VersionCode.HasValue)
            {
                newInfo.VersionCode = info.VersionCode.Value;
            }

            newInfo.Version = info.Version;
            newInfo.LocalPath = info.LocalPath;
            newInfo.AssetStoreInfo = ConvertToPublic(info.AssetStoreInfo);

            return newInfo;
        }

        static AssetStoreInfo ConvertToPublic(AssetStoreInfoInternal info)
        {
            if (info == null)
            {
                return null;
            }

            var newInfo = ScriptableObject.CreateInstance<AssetStoreInfo>();

            newInfo.PublisherId = info.PublisherId;
            newInfo.PublisherLabel = info.PublisherLabel;
            newInfo.PackageId = info.PackageId;
            newInfo.PublishNotes = info.PublishNotes;
            newInfo.CategoryId = info.CategoryId;
            newInfo.CategoryLabel = info.CategoryLabel;
            newInfo.UploadId = info.UploadId;
            newInfo.Description = info.Description;
            newInfo.PublishDate = info.PublishDate.HasValue ? DateTimeToString(info.PublishDate.Value) : null;
            newInfo.UnityVersion = info.UnityVersion;
            newInfo.LinkId = info.LinkId;
            newInfo.LinkType = info.LinkType;

            return newInfo;
        }

        static string DateTimeToString(DateTime utcDate)
        {
            return "{0} ({1})".Fmt(DateTimeUtil.FormatPastDateAsRelative(utcDate), utcDate.ToLocalTime().ToString("d"));
        }

        // Yaml requires that we use properties, but Unity serialization requires
        // the opposite - that we use fields
        class ReleaseInfoInternal
        {
            public string Name
            {
                get;
                set;
            }

            public int? VersionCode
            {
                get;
                set;
            }

            public string Version
            {
                get;
                set;
            }

            public string LocalPath
            {
                get;
                set;
            }

            public AssetStoreInfoInternal AssetStoreInfo
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

        class AssetStoreInfoInternal
        {
            public string PublisherId
            {
                get;
                set;
            }

            public string PublisherLabel
            {
                get;
                set;
            }

            public string PackageId
            {
                get;
                set;
            }

            public string PublishNotes
            {
                get;
                set;
            }

            public string CategoryId
            {
                get;
                set;
            }

            public string CategoryLabel
            {
                get;
                set;
            }

            public string UploadId
            {
                get;
                set;
            }

            public string Description
            {
                get;
                set;
            }

            public DateTime? PublishDate
            {
                get;
                set;
            }

            public string UnityVersion
            {
                get;
                set;
            }

            public string LinkId
            {
                get;
                set;
            }

            public string LinkType
            {
                get;
                set;
            }
        }
    }
}
