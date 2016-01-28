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
    public class PmPackageViewHandler
    {
        readonly PmSettings _pmSettings;
        readonly PrjCommandHandler _prjCommandHandler;
        readonly PmPackageHandler _packageHandler;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmView _view;
        readonly EventManager _eventManager = new EventManager(null);

        public PmPackageViewHandler(
            PmView view,
            AsyncProcessor asyncProcessor,
            PmPackageHandler packageHandler,
            PrjCommandHandler prjCommandHandler,
            PmSettings pmSettings)
        {
            _pmSettings = pmSettings;
            _prjCommandHandler = prjCommandHandler;
            _packageHandler = packageHandler;
            _asyncProcessor = asyncProcessor;
            _view = view;
        }

        public void Initialize()
        {
            _view.AddContextMenuHandler(DragListTypes.Package, GetContextMenuItems);

            // Use EventQueueMode.LatestOnly to ensure we don't execute anything during the OnGUI event
            // This is important since OnGUI is called in multiple passes and we have to ensure that the same
            // controls are rendered each pass
            _view.ClickedRefreshPackages += _eventManager.Add(OnClickedRefreshPackages, EventQueueMode.LatestOnly);
            _view.ClickedCreateNewPackage += _eventManager.Add(OnClickedCreateNewPackage, EventQueueMode.LatestOnly);
        }

        public void Dispose()
        {
            _view.RemoveContextMenuHandler(DragListTypes.Package);

            _view.ClickedRefreshPackages -= _eventManager.Remove(OnClickedRefreshPackages);
            _view.ClickedCreateNewPackage -= _eventManager.Remove(OnClickedCreateNewPackage);

            _eventManager.AssertIsEmpty();
        }

        public void OnClickedRefreshPackages()
        {
            _asyncProcessor.Process(_packageHandler.RefreshPackagesAsync(), true, "Refreshing Packages");
        }

        public void OnClickedCreateNewPackage()
        {
            _asyncProcessor.Process(CreateNewPackageAsync());
        }

        IEnumerator CreateNewPackageAsync()
        {
            var userInput = _view.PromptForInput("Enter new package name:", "Untitled");

            yield return userInput;

            if (userInput.Current == null)
            {
                // User Cancelled
                yield break;
            }

            yield return _prjCommandHandler.ProcessPrjCommand(
                "Creating Package '{0}'".Fmt(userInput.Current), PrjHelper.CreatePackageAsync(userInput.Current));
            yield return _packageHandler.RefreshPackagesAsync();
        }

        List<PackageInfo> GetSelectedItems()
        {
            return _view.GetSelected(DragListTypes.Package)
                .Select(x => (PackageInfo)x.Model).ToList();
        }

        IEnumerable<ContextMenuItem> GetContextMenuItems()
        {
            var selected = GetSelectedItems();

            var singleInfo = selected.OnlyOrDefault();

            yield return new ContextMenuItem(
                !selected.IsEmpty(), "Delete", false, OnContextMenuDeleteSelected);

            yield return new ContextMenuItem(
                singleInfo != null, "Rename", false, OnContextMenuRenameSelected);

            yield return new ContextMenuItem(
                singleInfo != null, "Show In Explorer", false, OnContextMenuOpenPackageFolderForSelected);

            yield return new ContextMenuItem(
                singleInfo != null && HasPackageYaml(selected.Single()), "Edit " + ProjenyEditorUtil.PackageConfigFileName, false, OnContextMenuEditPackageYamlSelected);

            yield return new ContextMenuItem(
                singleInfo != null, "More Info...", false, OpenMoreInfoPopupForSelected);

            yield return new ContextMenuItem(
                singleInfo != null && !string.IsNullOrEmpty(singleInfo.InstallInfo.ReleaseInfo.AssetStoreInfo.LinkId), "Open In Asset Store", false, OpenSelectedInAssetStore);

            yield return new ContextMenuItem(
                true, "Refresh", false, OnContextMenuRefresh);

            yield return new ContextMenuItem(
                true, "New Package...", false, OnContextMenuNewPackage);

            yield return new ContextMenuItem(
                true, "Show UnityPackages Folder In Explorer", false, OnContextMenuOpenUnityPackagesFolderInExplorer);
        }

        void OnContextMenuOpenUnityPackagesFolderInExplorer()
        {
            _asyncProcessor.Process(
                _prjCommandHandler.ProcessPrjCommand(
                    "", PrjHelper.OpenPackagesFolderInExplorer()));
        }

        void OpenSelectedInAssetStore()
        {
            var selected = GetSelectedItems();

            Assert.IsEqual(selected.Count, 1);

            var info = selected.Single();

            var assetStoreInfo = info.InstallInfo.ReleaseInfo.AssetStoreInfo;
            PmViewHandlerCommon.OpenInAssetStore(assetStoreInfo.LinkType, assetStoreInfo.LinkId);
        }

        void OpenMoreInfoPopupForSelected()
        {
            var selected = GetSelectedItems();
            Assert.IsEqual(selected.Count, 1);

            var info = selected.Single();

            _asyncProcessor.Process(OpenMoreInfoPopup(info));
        }

        IEnumerator OpenMoreInfoPopup(PackageInfo info)
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

                var scrollViewRect = new Rect(
                    contentRect.xMin, contentRect.yMin, contentRect.width, contentRect.height - skin.MarginBottom - skin.OkButtonHeight - skin.OkButtonTopPadding);

                GUILayout.BeginArea(scrollViewRect);
                {
                    scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, skin.ScrollViewStyle, GUILayout.ExpandHeight(true));
                    {
                        GUILayout.Space(skin.ListPaddingTop);
                        GUILayout.Label("Package Info", skin.HeadingStyle);
                        GUILayout.Space(skin.HeadingBottomPadding);

                        PmViewHandlerCommon.DrawMoreInfoRow(skin, "Name", info.Name);
                        GUILayout.Space(skin.RowSpacing);
                        PmViewHandlerCommon.DrawMoreInfoRow(skin, "Path", info.Path);
                        GUILayout.Space(skin.RowSpacing);
                        PmViewHandlerCommon.DrawMoreInfoRow(skin, "Creation Date", !string.IsNullOrEmpty(info.InstallInfo.InstallDate) ? info.InstallInfo.InstallDate : PmViewHandlerCommon.NotAvailableLabel);

                        GUILayout.Space(skin.ListPaddingTop);
                        GUILayout.Space(skin.ListPaddingTop);
                        GUILayout.Label("Release Info", skin.HeadingStyle);
                        GUILayout.Space(skin.HeadingBottomPadding);

                        if (string.IsNullOrEmpty(info.InstallInfo.ReleaseInfo.Id))
                        {
                            GUI.color = skin.ValueStyle.normal.textColor;
                            GUILayout.Label("No release is associated with this package", skin.HeadingStyle);
                            GUI.color = Color.white;
                        }
                        else
                        {
                            PmViewHandlerCommon.AddReleaseInfoMoreInfoRows(info.InstallInfo.ReleaseInfo, skin);
                        }

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

        void OnContextMenuNewPackage()
        {
            _asyncProcessor.Process(CreateNewPackageAsync());
        }

        void OnContextMenuRefresh()
        {
            _asyncProcessor.Process(
                _packageHandler.RefreshPackagesAsync(), true, "Refreshing Packages");
        }

        void OnContextMenuEditPackageYamlSelected()
        {
            var selected = GetSelectedItems();
            Assert.IsEqual(selected.Count, 1);

            var info = selected.Single();

            var configPath = Path.Combine(info.Path, ProjenyEditorUtil.PackageConfigFileName);

            Assert.That(File.Exists(configPath));

            InternalEditorUtility.OpenFileAtLineExternal(configPath, 1);
        }

        void OnContextMenuOpenPackageFolderForSelected()
        {
            var selected = GetSelectedItems();
            Assert.IsEqual(selected.Count, 1);

            var info = selected.Single();

            Assert.That(Directory.Exists(info.Path));

            System.Diagnostics.Process.Start(info.Path);
        }

        void OnContextMenuRenameSelected()
        {
            var selected = GetSelectedItems();
            Assert.IsEqual(selected.Count, 1);

            var info = selected.Single();

            _asyncProcessor.Process(RenamePackageAsync(info));
        }

        IEnumerator RenamePackageAsync(PackageInfo info)
        {
            var newPackageName = _view.PromptForInput("Enter package name:", info.Name);

            yield return newPackageName;

            if (newPackageName.Current == null)
            {
                // User Cancelled
                yield break;
            }

            if (newPackageName.Current == info.Name)
            {
                yield break;
            }

            var dirInfo = new DirectoryInfo(info.Path);
            Assert.That(dirInfo.Name == info.Name);

            var newPath = Path.Combine(dirInfo.Parent.FullName, newPackageName.Current);

            Assert.That(!Directory.Exists(newPath), "Package with name '{0}' already exists.  Rename aborted.", newPackageName.Current);

            dirInfo.MoveTo(newPath);

            yield return _packageHandler.RefreshPackagesAsync();

            _view.ClearSelected();
            _view.GetList(DragListTypes.Package).Values
                .Where(x => ((PackageInfo)x.Model).Name == newPackageName.Current).Single().IsSelected = true;
        }

        void OnContextMenuDeleteSelected()
        {
            var selected = GetSelectedItems();
            _asyncProcessor.Process(_packageHandler.DeletePackages(selected), true, "Deleting Packages");
        }

        bool HasPackageYaml(PackageInfo info)
        {
            var configPath = Path.Combine(info.Path, ProjenyEditorUtil.PackageConfigFileName);
            return File.Exists(configPath);
        }

        public void Update()
        {
            _eventManager.Flush();
        }
    }
}

