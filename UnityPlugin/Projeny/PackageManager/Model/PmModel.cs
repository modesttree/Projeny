using System;
using System.IO;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Projeny.Internal;
using System.Linq;

namespace Projeny.Internal
{
    public enum PmViewStates
    {
        ReleasesAndPackages,
        PackagesAndProject,
        Project,
        ProjectAndVisualStudio,
    }

    [Serializable]
    public class PmModel
    {
        public event Action PluginItemsChanged = delegate {};
        public event Action AssetItemsChanged = delegate {};
        public event Action PackageFoldersChanged = delegate {};
        public event Action ReleasesChanged = delegate {};
        public event Action VsProjectsChanged = delegate {};
        public event Action PackageFolderIndexChanged = delegate {};

        [SerializeField]
        List<PackageFolderInfo> _folderInfos = new List<PackageFolderInfo>();

        [SerializeField]
        List<ReleaseInfo> _releases = new List<ReleaseInfo>();

        [SerializeField]
        List<string> _assetItems = new List<string>();

        [SerializeField]
        List<string> _pluginItems = new List<string>();

        [SerializeField]
        List<string> _vsProjects = new List<string>();

        [SerializeField]
        List<string> _savedPackageFolders = new List<string>();

        [SerializeField]
        List<string> _prebuilt = new List<string>();

        [SerializeField]
        Dictionary<string, string> _solutionFolders = new Dictionary<string, string>();

        [SerializeField]
        string _projectSettingsPath;

        [SerializeField]
        int _packageFolderIndex;

        public PmModel()
        {
        }

        public int PackageFolderIndex
        {
            get
            {
                return _packageFolderIndex;
            }
            set
            {
                if (_packageFolderIndex != value)
                {
                    _packageFolderIndex = value;
                    PackageFolderIndexChanged();
                }
            }
        }

        public string ProjectSettingsPath
        {
            get
            {
                return _projectSettingsPath;
            }
            set
            {
                _projectSettingsPath = value;
            }
        }

        public IEnumerable<ReleaseInfo> Releases
        {
            get
            {
                return _releases;
            }
        }

        public IEnumerable<string> AssetItems
        {
            get
            {
                return _assetItems;
            }
        }

        public IEnumerable<PackageInfo> AllPackages
        {
            get
            {
                return _folderInfos.SelectMany(x => x.Packages);
            }
        }

        public IEnumerable<string> PluginItems
        {
            get
            {
                return _pluginItems;
            }
        }

        public IEnumerable<PackageFolderInfo> PackageFolders
        {
            get
            {
                return _folderInfos;
            }
        }

        public IEnumerable<string> PrebuiltProjects
        {
            get
            {
                return _prebuilt;
            }
        }

        public IEnumerable<string> VsProjects
        {
            get
            {
                return _vsProjects;
            }
        }

        public IEnumerable<KeyValuePair<string, string>> VsSolutionFolders
        {
            get
            {
                return _solutionFolders;
            }
        }

        public IEnumerable<string> SavedPackageFolders
        {
            get
            {
                return _savedPackageFolders;
            }
        }

        public string GetCurrentPackageFolderPath()
        {
            var folderPath = TryGetCurrentPackageFolderPath();
            Assert.IsNotNull(folderPath, "Could not find current package root folder path");
            return folderPath;
        }

        public string TryGetCurrentPackageFolderPath()
        {
            if (_packageFolderIndex >= 0 && _packageFolderIndex < _folderInfos.Count)
            {
                return _folderInfos[_packageFolderIndex].Path;
            }

            return null;
        }

        public IEnumerable<PackageInfo> GetCurrentFolderPackages()
        {
            if (_packageFolderIndex >= 0 && _packageFolderIndex < _folderInfos.Count)
            {
                return _folderInfos[_packageFolderIndex].Packages;
            }

            return Enumerable.Empty<PackageInfo>();
        }

        public void ClearSavedPackageFolders()
        {
            _savedPackageFolders.Clear();
        }

        public void ClearSolutionFolders()
        {
            _solutionFolders.Clear();
        }

        public void ClearPrebuiltProjects()
        {
            _prebuilt.Clear();
        }

        public void ClearSolutionProjects()
        {
            _vsProjects.Clear();
            VsProjectsChanged();
        }

        public void ClearAssetItems()
        {
            _assetItems.Clear();
            AssetItemsChanged();
        }

        public void RemoveVsProject(string name)
        {
            _vsProjects.RemoveWithConfirm(name);
            VsProjectsChanged();
        }

        public void RemoveAssetItem(string name)
        {
            _assetItems.RemoveWithConfirm(name);
            AssetItemsChanged();
        }

        public bool HasAssetItem(string name)
        {
            return _assetItems.Contains(name);
        }

        public bool HasVsProject(string name)
        {
            return _vsProjects.Contains(name);
        }

        public bool HasPluginItem(string name)
        {
            return _pluginItems.Contains(name);
        }

        public void RemovePluginItem(string name)
        {
            _pluginItems.RemoveWithConfirm(name);
            PluginItemsChanged();
        }

        public void AddVsProject(string name)
        {
            _vsProjects.Add(name);
            VsProjectsChanged();
        }

        public void AddPrebuilt(string value)
        {
            _prebuilt.Add(value);
        }

        public void AddSavedPackageFolder(string value)
        {
            _savedPackageFolders.Add(value);
        }

        public void AddSolutionFolder(string key, string value)
        {
            _solutionFolders.Add(key, value);
        }

        public void AddAssetItem(string name)
        {
            _assetItems.Add(name);
            AssetItemsChanged();
        }

        public void AddPluginItem(string name)
        {
            _pluginItems.Add(name);
            PluginItemsChanged();
        }

        public void ClearPluginItems()
        {
            _pluginItems.Clear();
            PluginItemsChanged();
        }

        public void SetPackageFolders(List<PackageFolderInfo> folderInfos)
        {
            _folderInfos.Clear();
            _folderInfos.AddRange(folderInfos);
            PackageFoldersChanged();
        }

        public void SetReleases(List<ReleaseInfo> releases)
        {
            Assert.That(releases.All(x => !string.IsNullOrEmpty(x.Name)));
            _releases.Clear();
            _releases.AddRange(releases);
            ReleasesChanged();
        }

        public bool IsPackageAddedToProject(string name)
        {
            return _assetItems.Concat(_pluginItems).Contains(name);
        }

        public bool IsReleaseInstalled(ReleaseInfo info)
        {
            return AllPackages
                .Any(x => x.InstallInfo != null
                        && x.InstallInfo.ReleaseInfo != null
                        && x.InstallInfo.ReleaseInfo.Id == info.Id
                        && x.InstallInfo.ReleaseInfo.VersionCode == info.VersionCode);
        }
    }
}

