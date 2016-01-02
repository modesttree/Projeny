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
    public class PmModelSyncer : IDisposable
    {
        readonly PmModel _model;
        readonly PmView _view;

        readonly EventManager _eventManager = new EventManager();

        public PmModelSyncer(
            PmModel model, PmView view)
        {
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            _model.ViewStateChanged += _eventManager.Add(OnViewStateChanged, EventQueueMode.LatestOnly);
            _model.PluginItemsChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
            _model.AssetItemsChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);
            _model.PackagesChanged += _eventManager.Add(OnListDisplayValuesDirty, EventQueueMode.LatestOnly);

            _model.ProjectConfigTypeChanged += _eventManager.Add(OnProjectConfigTypeChanged, EventQueueMode.LatestOnly);

            _eventManager.Trigger(OnViewStateChanged);
            _eventManager.Trigger(OnListDisplayValuesDirty);
            _eventManager.Trigger(OnProjectConfigTypeChanged);
        }

        public void Dispose()
        {
            _model.ViewStateChanged -= _eventManager.Remove(OnViewStateChanged);
            _model.PluginItemsChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
            _model.AssetItemsChanged -= _eventManager.Remove(OnListDisplayValuesDirty);
            _model.PackagesChanged -= _eventManager.Remove(OnListDisplayValuesDirty);

            _model.ProjectConfigTypeChanged -= _eventManager.Remove(OnProjectConfigTypeChanged);

            _eventManager.AssertIsEmpty();
        }

        public void Update()
        {
            _eventManager.Flush();
        }

        void OnProjectConfigTypeChanged()
        {
            _view.ConfigType = _model.ProjectConfigType;
        }

        void OnListDisplayValuesDirty()
        {
            _view.SetPluginItems(_model.PluginItems
                .Select(x => CreateListItemForProjectItem(x)).ToList());

            _view.SetAssetItems(_model.AssetItems
                .Select(x => CreateListItemForProjectItem(x)).ToList());

            _view.SetPackages(_model.Packages
                .Select(x => CreateListItem(x)).ToList());
        }

        PmView.ListItemData CreateListItemForProjectItem(string name)
        {
            string caption;

            if (_model.ViewState == PmViewStates.PackagesAndProject)
            {
                caption = ImguiUtil.WrapWithColor(name, _view.Skin.Theme.DraggableItemAlreadyAddedColor);
            }
            else
            {
                // this isn't always the case since it can be rendered when interpolating
                //Assert.That(_viewState == PmViewStates.Project);
                caption = name;
            }

            return new PmView.ListItemData()
            {
                Caption = caption,
                Tag = name
            };
        }

        PmView.ListItemData CreateListItem(PackageInfo info)
        {
            if (_model.ViewState == PmViewStates.ReleasesAndPackages)
            {
                Assert.Throw("TODO");
                //if (info.InstallInfo != null && info.InstallInfo.ReleaseInfo != null)
                //{
                    //var releaseInfo = info.InstallInfo.ReleaseInfo;

                    //var displayValue = "{0} ({1}{2})".Fmt(
                        //info.Name,
                        //WrapWithColor(releaseInfo.Name, Skin.Theme.DraggableItemAlreadyAddedColor),
                        //string.IsNullOrEmpty(releaseInfo.Version) ? "" : WrapWithColor(" v" + releaseInfo.Version, Skin.Theme.VersionColor));

                    //GUI.Label(rect, displayValue, Skin.ItemTextStyle);
                //}
                //else
                //{
                    //DrawListItem(rect, info.Name);
                //}
            }

            // this isn't always the case since it can be rendered when interpolating
            //Assert.IsEqual(_model.ViewState, PmViewStates.PackagesAndProject);

            string caption;

            if (_model.IsPackageAddedToProject(info.Name))
            {
                caption = ImguiUtil.WrapWithColor(
                    info.Name, _view.Skin.Theme.DraggableItemAlreadyAddedColor);
            }
            else
            {
                caption = info.Name;
            }

            return new PmView.ListItemData()
            {
                Caption = caption,
                Tag = info
            };
        }

        void OnViewStateChanged()
        {
            _view.ViewState = _model.ViewState;
        }
    }
}
