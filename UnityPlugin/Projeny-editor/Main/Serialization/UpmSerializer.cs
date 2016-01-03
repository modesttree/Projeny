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

            var newInfo = new PackageInfo();

            newInfo.Name = info.Name;
            newInfo.Path = info.Path;
            newInfo.InstallInfo = ConvertToPublic(info.InstallInfo);

            return newInfo;
        }

        static PackageInstallInfo ConvertToPublic(PackageInstallInfoInternal info)
        {
            if (info == null)
            {
                // Can't return null here since unity serialization doesn't support null
                return new PackageInstallInfo()
                {
                    ReleaseInfo = ConvertToPublic((ReleaseInfoInternal)null)
                };
            }

            var newInfo = new PackageInstallInfo();

            newInfo.InstallDate = DateTimeToString(info.InstallDate);
            newInfo.InstallDateTicks = info.InstallDate.Ticks;
            newInfo.ReleaseInfo = ConvertToPublic(info.ReleaseInfo);

            return newInfo;
        }

        static ReleaseInfo ConvertToPublic(ReleaseInfoInternal info)
        {
            if (info == null)
            {
                // Can't return null here since unity serialization doesn't support null
                return new ReleaseInfo()
                {
                    AssetStoreInfo = ConvertToPublic((AssetStoreInfoInternal)null),
                };
            }

            var newInfo = new ReleaseInfo();

            newInfo.Name = info.Name;

            newInfo.HasVersionCode = info.VersionCode.HasValue;
            if (info.VersionCode.HasValue)
            {
                newInfo.VersionCode = info.VersionCode.Value;
            }

            newInfo.HasCompressedSize = info.CompressedSize.HasValue;

            if (info.CompressedSize.HasValue)
            {
                newInfo.CompressedSize = info.CompressedSize.Value;
            }

            newInfo.Version = info.Version;
            newInfo.LocalPath = info.LocalPath;

            Assert.That(!string.IsNullOrEmpty(info.Id));
            newInfo.Id = info.Id;

            newInfo.FileModificationDate = info.FileModificationDate.HasValue ? DateTimeToString(info.FileModificationDate.Value) : null;
            newInfo.FileModificationDateTicks = info.FileModificationDate.HasValue ? info.FileModificationDate.Value.Ticks : 0;

            newInfo.AssetStoreInfo = ConvertToPublic(info.AssetStoreInfo);

            return newInfo;
        }

        static AssetStoreInfo ConvertToPublic(AssetStoreInfoInternal info)
        {
            if (info == null)
            {
                // Can't return null here since unity serialization doesn't support null
                return new AssetStoreInfo();
            }

            var newInfo = new AssetStoreInfo();

            newInfo.PublisherId = info.PublisherId;
            newInfo.PublisherLabel = info.PublisherLabel;
            newInfo.PublishNotes = info.PublishNotes;
            newInfo.CategoryId = info.CategoryId;
            newInfo.CategoryLabel = info.CategoryLabel;
            newInfo.UploadId = info.UploadId;
            newInfo.Description = info.Description;

            newInfo.PublishDate = info.PublishDate.HasValue ? DateTimeToString(info.PublishDate.Value) : null;
            newInfo.PublishDateTicks = info.PublishDate.HasValue ? info.PublishDate.Value.Ticks : 0;

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
            public string Id
            {
                get;
                set;
            }

            public string Name
            {
                get;
                set;
            }

            public int? CompressedSize
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

            public DateTime? FileModificationDate
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

        class PackageInstallInfoInternal
        {
            public DateTime InstallDate
            {
                get;
                set;
            }

            public ReleaseInfoInternal ReleaseInfo
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

            public PackageInstallInfoInternal InstallInfo
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
