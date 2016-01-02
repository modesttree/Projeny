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
    public class PmPackageViewHandler
    {
        readonly PmPackageHandler _packageHandler;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmView _view;
        readonly EventManager _eventManager = new EventManager();

        public PmPackageViewHandler(
            PmView view,
            AsyncProcessor asyncProcessor,
            PmPackageHandler packageHandler)
        {
            _packageHandler = packageHandler;
            _asyncProcessor = asyncProcessor;
            _view = view;
        }

        public void Initialize()
        {
            _view.AddContextMenuHandler(ListTypes.Package, GetContextMenuItems);
        }

        public void Dispose()
        {
            _view.RemoveContextMenuHandler(ListTypes.Package);
        }

        List<PackageInfo> GetSelectedItems()
        {
            return _view.GetSelected(ListTypes.Package)
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
        }

        void OnContextMenuEditPackageYamlSelected()
        {
            Assert.Throw("TODO");
        }

        void OnContextMenuOpenPackageFolderForSelected()
        {
            Assert.Throw("TODO");
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

            Assert.Throw("TODO");
            //SelectInternal(_packagesList.Values
            //.Where(x => x.Name == newPackageName.Current).Single());
        }

        void OnContextMenuDeleteSelected()
        {
            var selected = GetSelectedItems();
            _asyncProcessor.Process(DeletePackages(selected));
        }

        IEnumerator DeletePackages(List<PackageInfo> packages)
        {
            var choice = _view.PromptForUserChoice(
                "<color=yellow>Are you sure you wish to delete the following packages?</color>\n\n{0}\n\n<color=yellow>Please note the following:</color>\n\n- This change is not undoable\n- Any changes that you've made since installing will be lost\n- Any projects or other packages that still depend on this package may be put in an invalid state by deleting it".Fmt(packages.Select(x => "- " + x.Name).Join("\n")),
                new[] { "Delete", "Cancel" }, null, "DeleteSelectedPopupTextStyle");

            yield return choice;

            if (choice.Current == 0)
            {
                yield return _packageHandler.DeletePackages(packages);
            }
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


