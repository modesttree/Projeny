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
        readonly PmModel _model;
        readonly PmView _view;

        readonly EventManager _eventManager = new EventManager();

        public PmModelViewSyncer(
            PmModel model, PmView view)
        {
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            _model.PluginItemsChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
            _model.AssetItemsChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
            _model.PackagesChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
            _model.ReleasesChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);

            _view.ViewStateChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
            _view.ReleasesSortMethodChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
            _view.ReleaseSortAscendingChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);

            _eventManager.Trigger(OnListDisplayValuesDirty);
        }

        public void Dispose()
        {
            _model.PluginItemsChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
            _model.AssetItemsChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
            _model.PackagesChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
            _model.ReleasesChanged -= _eventManager.Remove(OnListDisplayValuesDirty);

            _view.ViewStateChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
            _view.ReleasesSortMethodChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
            _view.ReleaseSortAscendingChanged -= _eventManager.Remove(OnListDisplayValuesDirty);

            _eventManager.AssertIsEmpty();
        }

        public void Update()
        {
            _eventManager.Flush();
        }

        void OnListDisplayValuesDirty()
        {
            Log.Trace("OnListDisplayValuesDirty called");

            _view.SetListItems(
                ListTypes.Release,
                OrderReleases().Select(x => CreateListItem(x)).ToList());

            _view.SetListItems(
                ListTypes.PluginItem,
                _model.PluginItems.OrderBy(x => x).Select(x => CreateListItemForProjectItem(x)).ToList());

            _view.SetListItems(
                ListTypes.AssetItem,
                _model.AssetItems.OrderBy(x => x).Select(x => CreateListItemForProjectItem(x)).ToList());

            _view.SetListItems(
                ListTypes.Package,
                _model.Packages.OrderBy(x => x.Name).Select(x => CreateListItem(x)).ToList());
        }

        IEnumerable<ReleaseInfo> OrderReleases()
        {
            if (_view.ReleaseSortAscending)
            {
                return _model.Releases.OrderBy(x => GetReleaseSortField(x));
            }

            return _model.Releases.OrderByDescending(x => GetReleaseSortField(x));
        }

        object GetReleaseSortField(ReleaseInfo info)
        {
            switch (_view.ReleasesSortMethod)
            {
                case ReleasesSortMethod.Name:
                {
                    return info.Name;
                }
                case ReleasesSortMethod.Size:
                {
                    return info.CompressedSize;
                }
                case ReleasesSortMethod.PublishDate:
                {
                    return info.AssetStoreInfo == null ? 0 : info.AssetStoreInfo.PublishDateTicks;
                }
            }

            Assert.Throw();
            return null;
        }

        ListItemData CreateListItemForProjectItem(string name)
        {
            string caption;

            if (_view.ViewState == PmViewStates.PackagesAndProject)
            {
                caption = ImguiUtil.WrapWithColor(name, _view.Skin.Theme.DraggableItemAlreadyAddedColor);
            }
            else
            {
                // this isn't always the case since it can be rendered when interpolating
                //Assert.That(_viewState == PmViewStates.Project);
                caption = name;
            }

            return new ListItemData()
            {
                Caption = caption,
                Model = name
            };
        }

        ListItemData CreateListItem(ReleaseInfo info)
        {
            string caption;

            if (_model.IsReleaseInstalled(info))
            {
                caption = ImguiUtil.WrapWithColor(
                    info.Name, _view.Skin.Theme.DraggableItemAlreadyAddedColor);
            }
            else
            {
                caption = info.Name;
            }

            caption = string.IsNullOrEmpty(info.Version) ? caption : "{0} {1}"
                .Fmt(caption, ImguiUtil.WrapWithColor("v" + info.Version, _view.Skin.Theme.VersionColor));

            return new ListItemData()
            {
                Caption = caption,
                Model = info,
            };
        }

        ListItemData CreateListItem(PackageInfo info)
        {
            string caption;

            if (_view.ViewState == PmViewStates.ReleasesAndPackages)
            {
                var releaseInfo = info.InstallInfo.ReleaseInfo;
                if (!string.IsNullOrEmpty(releaseInfo.Name))
                {
                    caption = "{0} ({1}{2})".Fmt(
                        info.Name,
                        ImguiUtil.WrapWithColor(releaseInfo.Name, _view.Skin.Theme.DraggableItemAlreadyAddedColor),
                        string.IsNullOrEmpty(releaseInfo.Version) ? "" : ImguiUtil.WrapWithColor(" v" + releaseInfo.Version, _view.Skin.Theme.VersionColor));
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
                        info.Name, _view.Skin.Theme.DraggableItemAlreadyAddedColor);
                }
                else
                {
                    caption = info.Name;
                }
            }

            return new ListItemData()
            {
                Caption = caption,
                Model = info
            };
        }
    }
}
