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
        readonly PrjCommandHandler _prjCommandHandler;
        readonly PmPackageHandler _packageHandler;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmView _view;
        readonly EventManager _eventManager = new EventManager();

        public PmPackageViewHandler(
            PmView view,
            AsyncProcessor asyncProcessor,
            PmPackageHandler packageHandler,
            PrjCommandHandler prjCommandHandler)
        {
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
            _asyncProcessor.Process(_packageHandler.RefreshPackagesAsync(), "Refreshing Packages");
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

            yield return new ContextMenuItem(
                !selected.IsEmpty(), "Delete", false, OnContextMenuDeleteSelected);

            yield return new ContextMenuItem(
                selected.Count == 1, "Rename", false, OnContextMenuRenameSelected);

            yield return new ContextMenuItem(
                selected.Count == 1, "Open Folder", false, OnContextMenuOpenPackageFolderForSelected);

            yield return new ContextMenuItem(
                selected.Count == 1 && HasPackageYaml(selected.Single()), "Edit " + ProjenyEditorUtil.PackageConfigFileName, false, OnContextMenuEditPackageYamlSelected);

            yield return new ContextMenuItem(
                true, "Refresh", false, OnContextMenuRefresh);

            yield return new ContextMenuItem(
                true, "New Package...", false, OnContextMenuNewPackage);
        }

        void OnContextMenuNewPackage()
        {
            _asyncProcessor.Process(CreateNewPackageAsync());
        }

        void OnContextMenuRefresh()
        {
            _asyncProcessor.Process(
                _packageHandler.RefreshPackagesAsync(), "Refreshing Packages");
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
            _asyncProcessor.Process(_packageHandler.DeletePackages(selected), "Deleting Packages");
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


