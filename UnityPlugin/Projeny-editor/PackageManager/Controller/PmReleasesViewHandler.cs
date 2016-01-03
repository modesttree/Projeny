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
    public class PmReleasesViewHandler : IDisposable
    {
        const string NotAvailableLabel = "N/A";

        readonly PmSettings _pmSettings;
        readonly UpmCommandHandler _upmCommandHandler;
        readonly PmPackageHandler _packageHandler;
        readonly PmReleasesHandler _releasesHandler;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmView _view;
        readonly PmModel _model;
        readonly EventManager _eventManager = new EventManager();

        public PmReleasesViewHandler(
            PmModel model,
            PmView view,
            AsyncProcessor asyncProcessor,
            PmReleasesHandler releasesHandler,
            PmPackageHandler packageHandler,
            UpmCommandHandler upmCommandHandler,
            PmSettings pmSettings)
        {
            _pmSettings = pmSettings;
            _upmCommandHandler = upmCommandHandler;
            _packageHandler = packageHandler;
            _releasesHandler = releasesHandler;
            _asyncProcessor = asyncProcessor;
            _view = view;
            _model = model;
        }

        public void Initialize()
        {
            _view.ClickedRefreshReleaseList += _eventManager.Add(OnClickedRefreshReleaseList, EventQueueMode.LatestOnly);

            _view.AddContextMenuHandler(DragListTypes.Release, GetContextMenuItems);
        }

        public void Dispose()
        {
            _view.ClickedRefreshReleaseList -= _eventManager.Remove(OnClickedRefreshReleaseList);
            _eventManager.AssertIsEmpty();

            _view.RemoveContextMenuHandler(DragListTypes.Release);
        }

        public void Update()
        {
            _eventManager.Flush();
        }

        List<ReleaseInfo> GetSelectedItems()
        {
            return _view.GetSelected(DragListTypes.Release)
                .Select(x => (ReleaseInfo)x.Model).ToList();
        }

        IEnumerable<ContextMenuItem> GetContextMenuItems()
        {
            var selected = GetSelectedItems();

            bool hasLocalPath = false;
            bool hasAssetStoreLink = false;

            var singleInfo = selected.OnlyOrDefault();

            if (singleInfo != null)
            {
                hasLocalPath = singleInfo.LocalPath != null && File.Exists(singleInfo.LocalPath);
                hasAssetStoreLink = singleInfo.AssetStoreInfo != null && !string.IsNullOrEmpty(singleInfo.AssetStoreInfo.LinkId);
            }

            yield return new ContextMenuItem(
                hasLocalPath, "Open Folder", false, OpenReleaseFolderForSelected);

            yield return new ContextMenuItem(
                singleInfo != null, "More Info...", false, OpenMoreInfoPopupForSelected);

            yield return new ContextMenuItem(
                hasAssetStoreLink, "Open In Asset Store", false, OpenSelectedInAssetStore);
        }

        void OpenSelectedInAssetStore()
        {
            var selected = GetSelectedItems();

            Assert.IsEqual(selected.Count, 1);

            var info = selected.Single();
            var assetStoreInfo = info.AssetStoreInfo;

            Assert.IsNotNull(assetStoreInfo);

            var fullUrl = "https://www.assetstore.unity3d.com/#/{0}/{1}".Fmt(assetStoreInfo.LinkType, assetStoreInfo.LinkId);
            Application.OpenURL(fullUrl);
        }

        void OpenMoreInfoPopupForSelected()
        {
            var selected = GetSelectedItems();
            Assert.IsEqual(selected.Count, 1);

            var info = selected.Single();

            _asyncProcessor.Process(OpenMoreInfoPopup(info));
        }

        IEnumerator OpenMoreInfoPopup(ReleaseInfo info)
        {
            bool isDone = false;

            var skin = _pmSettings.ReleaseMoreInfoDialog;
            Vector2 scrollPos = Vector2.zero;

            var popupId = _view.AddPopup(delegate(Rect fullRect)
            {
                var popupRect = ImguiUtil.CenterRectInRect(fullRect, skin.PopupSize);

                _view.DrawPopupCommon(fullRect, popupRect);

                var contentRect = ImguiUtil.CreateContentRectWithPadding(
                    popupRect, skin.PanelPadding);

                GUILayout.BeginArea(contentRect);
                {
                    GUILayout.Label("Release Info", skin.HeadingStyle);

                    GUILayout.Space(skin.HeadingBottomPadding);

                    scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, skin.ScrollViewStyle, GUILayout.Height(skin.ListHeight));
                    {
                        GUILayout.Space(skin.ListPaddingTop);

                        DrawMoreInfoRow(skin, "Name", info.Name);
                        GUILayout.Space(skin.RowSpacing);
                        DrawMoreInfoRow(skin, "Version", string.IsNullOrEmpty(info.Version) ? NotAvailableLabel : info.Version);
                        GUILayout.Space(skin.RowSpacing);
                        DrawMoreInfoRow(skin, "Publish Date", !string.IsNullOrEmpty(info.AssetStoreInfo.PublishDate) ? info.AssetStoreInfo.PublishDate : NotAvailableLabel);
                        GUILayout.Space(skin.RowSpacing);
                        DrawMoreInfoRow(skin, "Modification Date", !string.IsNullOrEmpty(info.FileModificationDate) ? info.FileModificationDate : NotAvailableLabel);
                        GUILayout.Space(skin.RowSpacing);
                        DrawMoreInfoRow(skin, "Compressed Size", info.HasCompressedSize ? MiscUtil.ConvertByteSizeToDisplayValue(info.CompressedSize) : NotAvailableLabel);
                        GUILayout.Space(skin.RowSpacing);
                        DrawMoreInfoRow(skin, "Publisher", !string.IsNullOrEmpty(info.AssetStoreInfo.PublisherLabel) ? info.AssetStoreInfo.PublisherLabel : NotAvailableLabel);
                        GUILayout.Space(skin.RowSpacing);
                        DrawMoreInfoRow(skin, "Category", !string.IsNullOrEmpty(info.AssetStoreInfo.CategoryLabel) ? info.AssetStoreInfo.CategoryLabel : NotAvailableLabel);
                        GUILayout.Space(skin.RowSpacing);
                        DrawMoreInfoRow(skin, "Description", !string.IsNullOrEmpty(info.AssetStoreInfo.Description) ? info.AssetStoreInfo.Description : NotAvailableLabel);
                        GUILayout.Space(skin.RowSpacing);
                        DrawMoreInfoRow(skin, "Unity Version", !string.IsNullOrEmpty(info.AssetStoreInfo.UnityVersion) ? info.AssetStoreInfo.UnityVersion : NotAvailableLabel);
                        GUILayout.Space(skin.RowSpacing);
                        DrawMoreInfoRow(skin, "ID", info.Id);
                        GUILayout.Space(skin.RowSpacing);
                        DrawMoreInfoRow(skin, "Publish Notes", !string.IsNullOrEmpty(info.AssetStoreInfo.PublishNotes) ? info.AssetStoreInfo.PublishNotes : NotAvailableLabel);
                        GUILayout.Space(skin.RowSpacing);
                        DrawMoreInfoRow(skin, "Version Code", info.HasVersionCode ? info.VersionCode.ToString() : NotAvailableLabel);
                        GUILayout.Space(skin.RowSpacing);
                    }
                    GUI.EndScrollView();
                }
                GUILayout.EndArea();

                var okButtonRect = new Rect(
                    contentRect.xMin + 0.5f * contentRect.width - 0.5f * skin.OkButtonWidth,
                    contentRect.yMax - skin.MarginBottom - skin.OkButtonHeight,
                    skin.OkButtonWidth,
                    skin.OkButtonHeight);

                if (GUI.Button(okButtonRect, "Ok") || Event.current.keyCode == KeyCode.Escape)
                {
                    isDone = true;
                }
            });

            while (!isDone)
            {
                yield return null;
            }

            _view.RemovePopup(popupId);
        }

        void DrawMoreInfoRow(PmSettings.ReleaseInfoMoreInfoDialogProperties skin, string label, string value)
        {
            GUILayout.BeginHorizontal();
            {
                if (value == NotAvailableLabel)
                {
                    GUI.color = skin.NotAvailableColor;
                }
                GUILayout.Label(label + ":", skin.LabelStyle, GUILayout.Width(skin.LabelColumnWidth));
                GUILayout.Space(skin.ColumnSpacing);
                GUILayout.Label(value, skin.ValueStyle, GUILayout.Width(skin.ValueColumnWidth));
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();
        }

        void OpenReleaseFolderForSelected()
        {
            var selected = GetSelectedItems();
            Assert.IsEqual(selected.Count, 1);

            var info = selected.Single();

            Assert.IsNotNull(info.LocalPath);
            PathUtil.AssertPathIsValid(info.LocalPath);

            var args = @"/select, " + info.LocalPath;
            System.Diagnostics.Process.Start("explorer.exe", args);
        }

        public void OnClickedRefreshReleaseList()
        {
            _asyncProcessor.Process(_releasesHandler.RefreshReleasesAsync(), "Refreshing Release List");
        }
    }
}

