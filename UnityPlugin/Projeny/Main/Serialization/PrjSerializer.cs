using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Projeny.Internal
{
    public static class PrjSerializer
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

        public static PackageFolderInfo DeserializePackageFolderInfo(string yamlStr)
        {
            return ConvertToPublic(YamlSerializer.Deserialize<PackageFolderInfoInternal>(yamlStr));
        }

        static ProjectConfigInternal ConvertToInternal(ProjectConfig info)
        {
            return new ProjectConfigInternal()
            {
                ProjectSettingsPath = info.ProjectSettingsPath,
                AssetsFolder = info.AssetsFolder.IsEmpty() ? null : info.AssetsFolder.ToList(),
                PluginsFolder = info.PluginsFolder.IsEmpty() ? null : info.PluginsFolder.ToList(),
                SolutionProjects = info.SolutionProjects.IsEmpty() ? null : info.SolutionProjects.ToList(),
                PackageFolders = info.PackageFolders.IsEmpty() ? null : info.PackageFolders.ToList(),
                Prebuilt = info.Prebuilt.IsEmpty() ? null : info.Prebuilt.ToList(),
                SolutionFolders = info.SolutionFolders.IsEmpty() ? null : info.SolutionFolders.Select(x => new Dictionary<string, string>() { { x.Key, x.Value } } ).ToList(),
            };
        }

        static ProjectConfig ConvertToPublic(ProjectConfigInternal info)
        {
            if (info == null)
            {
                return null;
            }

            var newInfo = new ProjectConfig();

            newInfo.ProjectSettingsPath = info.ProjectSettingsPath;

            if (info.AssetsFolder != null)
            {
                newInfo.AssetsFolder.AddRange(info.AssetsFolder.ToList());
            }

            if (info.PluginsFolder != null)
            {
                newInfo.PluginsFolder.AddRange(info.PluginsFolder.ToList());
            }

            if (info.SolutionProjects != null)
            {
                newInfo.SolutionProjects.AddRange(info.SolutionProjects.ToList());
            }

            if (info.PackageFolders != null)
            {
                newInfo.PackageFolders.AddRange(info.PackageFolders.ToList());
            }

            if (info.Prebuilt != null)
            {
                newInfo.Prebuilt.AddRange(info.Prebuilt.ToList());
            }

            if (info.SolutionFolders != null)
            {
                newInfo.SolutionFolders.AddRange(info.SolutionFolders.Select(x => x.Single()).ToList());
            }

            return newInfo;
        }

        static PackageFolderInfo ConvertToPublic(PackageFolderInfoInternal info)
        {
            if (info == null)
            {
                return null;
            }

            var newInfo = new PackageFolderInfo();

            newInfo.Path = info.Path;

            if (info.Packages != null)
            {
                foreach (var packageInfo in info.Packages)
                {
                    var newPackageInfo = new PackageInfo();

                    newPackageInfo.Name = packageInfo.Name;
                    newPackageInfo.InstallInfo = ConvertToPublic(packageInfo.InstallInfo);
                    newPackageInfo.FullPath = Path.Combine(info.Path, packageInfo.Name);

                    newInfo.Packages.Add(newPackageInfo);
                }
            }

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
            newInfo.Url = info.Url;

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

            public long? CompressedSize
            {
                get;
                set;
            }

            public long? VersionCode
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

            public string Url
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
            public string ProjectSettingsPath
            {
                get;
                set;
            }

            public List<string> AssetsFolder
            {
                get;
                set;
            }

            public List<string> PluginsFolder
            {
                get;
                set;
            }

            public List<string> PackageFolders
            {
                get;
                set;
            }

            public List<string> SolutionProjects
            {
                get;
                set;
            }

            public List<string> Prebuilt
            {
                get;
                set;
            }

            public List<Dictionary<string, string>> SolutionFolders
            {
                get;
                set;
            }

            public List<string> TargetPlatforms
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

        class PackageFolderInfoInternal
        {
            public string Path
            {
                get;
                set;
            }

            public List<PackageInfoInternal> Packages
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
