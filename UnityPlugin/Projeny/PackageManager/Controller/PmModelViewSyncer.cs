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
    public class PmModelViewSyncer : IDisposable
    {
        readonly PmSettings _pmSettings;
        readonly PmModel _model;
        readonly PmView _view;

        readonly EventManager _eventManager = new EventManager(null);

        public PmModelViewSyncer(
            PmModel model, PmView view,
            PmSettings pmSettings)
        {
            _pmSettings = pmSettings;
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            _model.PluginItemsChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
            _model.AssetItemsChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
            _model.PackagesChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
            _model.ReleasesChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
            _model.VsProjectsChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);

            _view.ViewStateChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);

            foreach (var list in _view.Lists)
            {
                list.SortDescendingChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
                list.SortMethodChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
            }

            // Don't bother showing the search pane for assets / plugins  - Or is that useful?

            var releaseList = _view.GetList(DragListTypes.Release);
            releaseList.ShowSortPane = true;
            releaseList.SortMethodCaptions = new List<string>()
            {
                // These should match ReleasesSortMethod
                "Order By Name",
                "Order By File Modification Time",
                "Order By Size",
                "Order By Release Date"
            };

            var packagesList = _view.GetList(DragListTypes.Package);
            packagesList.ShowSortPane = true;
            packagesList.SortMethodCaptions = new List<string>()
            {
                // These should match PackagesSortMethod
                "Order By Name",
                "Order By Install Date",
                "Order By Release Date"
            };

            _eventManager.Trigger(OnListDisplayValuesDirty);
        }

        public void Dispose()
        {
            _model.PluginItemsChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
            _model.AssetItemsChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
            _model.PackagesChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
            _model.ReleasesChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
            _model.VsProjectsChanged -= _eventManager.Remove(OnListDisplayValuesDirty);

            foreach (var list in _view.Lists)
            {
                list.SortDescendingChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
                list.SortMethodChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
            }

            _view.ViewStateChanged -= _eventManager.Remove(OnListDisplayValuesDirty);

            _eventManager.AssertIsEmpty();
        }

        public void Update()
        {
            _eventManager.Flush();
        }

        void OnListDisplayValuesDirty()
        {
            _view.SetListItems(
                DragListTypes.Release,
                OrderReleases().Select(x => CreateListItem(x)).ToList());

            _view.SetListItems(
                DragListTypes.PluginItem,
                OrderPluginItems().Select(x => CreateListItemForProjectItem(x)).ToList());

            _view.SetListItems(
                DragListTypes.AssetItem,
                OrderAssetItems().Select(x => CreateListItemForProjectItem(x)).ToList());

            _view.SetListItems(
                DragListTypes.Package,
                OrderPackages().Select(x => CreateListItem(x)).ToList());

            _view.SetListItems(
                DragListTypes.VsSolution,
                OrderVsProjects().Select(x => CreateListItemForVsProject(x)).ToList());
        }

        IEnumerable<string> OrderVsProjects()
        {
            if (_view.GetList(DragListTypes.VsSolution).SortDescending)
            {
                return _model.VsProjects.OrderByDescending(x => x);
            }

            return _model.VsProjects.OrderBy(x => x);
        }

        IEnumerable<string> OrderAssetItems()
        {
            if (_view.GetList(DragListTypes.AssetItem).SortDescending)
            {
                return _model.AssetItems.OrderByDescending(x => x);
            }

            return _model.AssetItems.OrderBy(x => x);
        }

        IEnumerable<string> OrderPluginItems()
        {
            if (_view.GetList(DragListTypes.PluginItem).SortDescending)
            {
                return _model.PluginItems.OrderByDescending(x => x);
            }

            return _model.PluginItems.OrderBy(x => x);
        }

        IEnumerable<ReleaseInfo> OrderReleases()
        {
            if (_view.GetList(DragListTypes.Release).SortDescending)
            {
                return _model.Releases.OrderByDescending(x => GetReleaseSortField(x));
            }

            return _model.Releases.OrderBy(x => GetReleaseSortField(x));
        }

        IEnumerable<PackageInfo> OrderPackages()
        {
            if (_view.GetList(DragListTypes.Package).SortDescending)
            {
                return _model.Packages.OrderByDescending(x => GetPackageSortField(x));
            }

            return _model.Packages.OrderBy(x => GetPackageSortField(x));
        }

        object GetPackageSortField(PackageInfo info)
        {
            switch ((PackagesSortMethod)_view.GetList(DragListTypes.Package).SortMethod)
            {
                case PackagesSortMethod.Name:
                {
                    return info.Name;
                }
                case PackagesSortMethod.InstallDate:
                {
                    return info.InstallInfo.InstallDateTicks;
                }
                case PackagesSortMethod.ReleasePublishDate:
                {
                    return info.InstallInfo.ReleaseInfo.AssetStoreInfo.PublishDateTicks;
                }
            }

            Assert.Throw();
            return null;
        }

        object GetReleaseSortField(ReleaseInfo info)
        {
            switch ((ReleasesSortMethod)_view.GetList(DragListTypes.Release).SortMethod)
            {
                case ReleasesSortMethod.Name:
                {
                    return info.Name;
                }
                case ReleasesSortMethod.FileModificationDate:
                {
                    return info.FileModificationDateTicks;
                }
                case ReleasesSortMethod.Size:
                {
                    return info.CompressedSize;
                }
                case ReleasesSortMethod.ReleaseDate:
                {
                    return info.AssetStoreInfo.PublishDateTicks;
                }
            }

            Assert.Throw();
            return null;
        }

        DragList.ItemDescriptor CreateListItemForVsProject(string name)
        {
            string caption;

            if (_model.HasAssetItem(name) || _model.HasPluginItem(name))
            {
                caption = ImguiUtil.WrapWithColor(
                    name, _pmSettings.View.Theme.DraggableItemAlreadyAddedColor);
            }
            else
            {
                caption = name;
            }

            return new DragList.ItemDescriptor()
            {
                Caption = caption,
                Model = name
            };
        }

        DragList.ItemDescriptor CreateListItemForProjectItem(string name)
        {
            string caption;

            if (_view.ViewState == PmViewStates.PackagesAndProject
                || (_view.ViewState == PmViewStates.ProjectAndVisualStudio && _model.HasVsProject(name)))
            {
                caption = ImguiUtil.WrapWithColor(name, _pmSettings.View.Theme.DraggableItemAlreadyAddedColor);
            }
            else
            {
                // this isn't always the case since it can be rendered when interpolating
                //Assert.That(_viewState == PmViewStates.Project);
                caption = name;
            }

            return new DragList.ItemDescriptor()
            {
                Caption = caption,
                Model = name
            };
        }

        DragList.ItemDescriptor CreateListItem(ReleaseInfo info)
        {
            string caption;

            if (_model.IsReleaseInstalled(info))
            {
                caption = ImguiUtil.WrapWithColor(
                    info.Name, _pmSettings.View.Theme.DraggableItemAlreadyAddedColor);
            }
            else
            {
                caption = info.Name;
            }

            caption = string.IsNullOrEmpty(info.Version) ? caption : "{0} {1}"
                .Fmt(caption, ImguiUtil.WrapWithColor("v" + info.Version, _pmSettings.View.Theme.VersionColor));

            return new DragList.ItemDescriptor()
            {
                Caption = caption,
                Model = info,
            };
        }

        DragList.ItemDescriptor CreateListItem(PackageInfo info)
        {
            string caption;

            if (_view.ViewState == PmViewStates.ReleasesAndPackages)
            {
                var releaseInfo = info.InstallInfo.ReleaseInfo;
                if (!string.IsNullOrEmpty(releaseInfo.Name))
                {
                    caption = "{0} ({1}{2})".Fmt(
                        info.Name,
                        ImguiUtil.WrapWithColor(releaseInfo.Name, _pmSettings.View.Theme.DraggableItemAlreadyAddedColor),
                        string.IsNullOrEmpty(releaseInfo.Version) ? "" : ImguiUtil.WrapWithColor(" v" + releaseInfo.Version, _pmSettings.View.Theme.VersionColor));
                }
                else
                {
                    caption = info.Name;
                }
            }
            else
            {
                // this isn't always the case since it can be rendered when interpolating
                //Assert.IsEqual(_model.ViewState, PmViewStates.PackagesAndProject);

                if (_model.IsPackageAddedToProject(info.Name))
                {
                    caption = ImguiUtil.WrapWithColor(
                        info.Name, _pmSettings.View.Theme.DraggableItemAlreadyAddedColor);
                }
                else
                {
                    caption = info.Name;
                }
            }

            return new DragList.ItemDescriptor()
            {
                Caption = caption,
                Model = info
            };
        }

        public enum PackagesSortMethod
        {
            Name,
            InstallDate,
            ReleasePublishDate
        }

        public enum ReleasesSortMethod
        {
            Name,
            FileModificationDate,
            Size,
            ReleaseDate
        }
    }
}
