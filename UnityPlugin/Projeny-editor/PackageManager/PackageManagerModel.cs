using System;
using System.IO;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Projeny.Internal;
using System.Linq;

namespace Projeny
{
    public enum PackageManagerViewStates
    {
        ReleasesAndPackages,
        PackagesAndProject,
        Project,
    }

    public enum ReleasesSortMethod
    {
        Name,
        Size,
        PublishDate
    }



    [Serializable]
    public class PackageManagerModel
    {
        public event Action ViewStateChanged = delegate {};
        public event Action PluginItemsChanged = delegate {};
        public event Action AssetItemsChanged = delegate {};
        public event Action PackagesChanged = delegate {};

        PackageManagerViewStates _viewState = PackageManagerViewStates.PackagesAndProject;

        ProjectConfigTypes _projectConfigType = ProjectConfigTypes.LocalProject;

        ReleasesSortMethod _releasesSortMethod;
        bool _releaseSortAscending;

        readonly List<PackageInfo> _allPackages = new List<PackageInfo>();
        readonly List<ReleaseInfo> _allReleases = new List<ReleaseInfo>();
        readonly List<string> _assetItems = new List<string>();
        readonly List<string> _pluginItems = new List<string>();

        public PackageManagerModel()
        {
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
                return _allPackages;
            }
        }

        public bool ReleaseSortAscending
        {
            get
            {
                return _releaseSortAscending;
            }
            set
            {
                _releaseSortAscending = value;
            }
        }

        public ReleasesSortMethod ReleasesSortMethod
        {
            get
            {
                return _releasesSortMethod;
            }
            set
            {
                _releasesSortMethod = value;
            }
        }

        public ProjectConfigTypes ProjectConfigType
        {
            get
            {
                return _projectConfigType;
            }
        }

        public PackageManagerViewStates ViewState
        {
            get
            {
                return _viewState;
            }
            set
            {
                if (_viewState != value)
                {
                    _viewState = value;
                    ViewStateChanged();
                }
            }
        }

        public void ClearAssetItems()
        {
            _assetItems.Clear();
            AssetItemsChanged();
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

        public bool HasPluginItem(string name)
        {
            return _pluginItems.Contains(name);
        }

        public void RemovePluginItem(string name)
        {
            _pluginItems.RemoveWithConfirm(name);
            PluginItemsChanged();
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
            _allPackages.Clear();
            _allPackages.AddRange(packages);
            PackagesChanged();
        }

        public void SetReleases(List<ReleaseInfo> releases)
        {
            _allReleases.Clear();
            _allReleases.AddRange(releases);
        }

        public bool IsPackageAddedToProject(string name)
        {
            return _assetItems.Concat(_pluginItems).Contains(name);
        }

        public bool IsReleaseInstalled(ReleaseInfo info)
        {
            return _allPackages
                .Any(x => x.InstallInfo != null
                        && x.InstallInfo.ReleaseInfo != null
                        && x.InstallInfo.ReleaseInfo.Id == info.Id
                        && x.InstallInfo.ReleaseInfo.VersionCode == info.VersionCode);
        }
    }
}

