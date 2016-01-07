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
        public event Action PackagesChanged = delegate {};
        public event Action ReleasesChanged = delegate {};
        public event Action VsProjectsChanged = delegate {};

        [SerializeField]
        List<PackageInfo> _packages = new List<PackageInfo>();

        [SerializeField]
        List<ReleaseInfo> _releases = new List<ReleaseInfo>();

        [SerializeField]
        List<string> _assetItems = new List<string>();

        [SerializeField]
        List<string> _pluginItems = new List<string>();

        [SerializeField]
        List<string> _vsProjects = new List<string>();

        public PmModel()
        {
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

        public IEnumerable<string> PluginItems
        {
            get
            {
                return _pluginItems;
            }
        }

        public IEnumerable<PackageInfo> Packages
        {
            get
            {
                return _packages;
            }
        }

        public IEnumerable<string> VsProjects
        {
            get
            {
                return _vsProjects;
            }
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

        public void SetPackages(List<PackageInfo> packages)
        {
            _packages.Clear();
            _packages.AddRange(packages);
            PackagesChanged();
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
            return _packages
                .Any(x => x.InstallInfo != null
                        && x.InstallInfo.ReleaseInfo != null
                        && x.InstallInfo.ReleaseInfo.Id == info.Id
                        && x.InstallInfo.ReleaseInfo.VersionCode == info.VersionCode);
        }
    }
}

