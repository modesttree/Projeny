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
    public class PmProjectViewHandler
    {
        readonly PmViewErrorHandler _errorHandler;
        readonly PrjCommandHandler _prjCommandHandler;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmModel _model;
        readonly PmView _view;

        readonly EventManager _eventManager = new EventManager();

        readonly PmProjectHandler _projectHandler;

        public PmProjectViewHandler(
            PmModel model,
            PmView view,
            PmProjectHandler projectHandler,
            AsyncProcessor asyncProcessor,
            PrjCommandHandler prjCommandHandler,
            PmViewErrorHandler errorHandler)
        {
            _errorHandler = errorHandler;
            _prjCommandHandler = prjCommandHandler;
            _asyncProcessor = asyncProcessor;
            _model = model;
            _view = view;
            _projectHandler = projectHandler;
        }

        public void Initialize()
        {
            _view.ClickedProjectType += _eventManager.Add<ProjectConfigTypes>(OnClickedProjectType, EventQueueMode.LatestOnly);

            _view.ClickedProjectApplyButton += _eventManager.Add(OnClickedProjectApplyButton, EventQueueMode.LatestOnly);

            _view.ClickedProjectRevertButton += _eventManager.Add(OnClickedProjectRevertButton, EventQueueMode.LatestOnly);
            _view.ClickedProjectSaveButton += _eventManager.Add(OnClickedProjectSaveButton, EventQueueMode.LatestOnly);
            _view.ClickedProjectEditButton += _eventManager.Add(OnClickedProjectEditButton, EventQueueMode.LatestOnly);

            _view.AddContextMenuHandler(DragListTypes.AssetItem, GetProjectItemContextMenu);
            _view.AddContextMenuHandler(DragListTypes.PluginItem, GetProjectItemContextMenu);

            _model.PluginItemsChanged += _eventManager.Add(OnProjectConfigDirty, EventQueueMode.LatestOnly);
            _model.AssetItemsChanged += _eventManager.Add(OnProjectConfigDirty, EventQueueMode.LatestOnly);
            _model.PackagesChanged += _eventManager.Add(OnProjectConfigDirty, EventQueueMode.LatestOnly);
            _model.ReleasesChanged += _eventManager.Add(OnProjectConfigDirty, EventQueueMode.LatestOnly);
            _model.VsProjectsChanged += _eventManager.Add(OnProjectConfigDirty, EventQueueMode.LatestOnly);

            _projectHandler.SavedConfigFile += _eventManager.Add(OnSavedConfigFile, EventQueueMode.LatestOnly);
            _projectHandler.LoadedConfigFile += _eventManager.Add(OnLoadedConfigFile, EventQueueMode.LatestOnly);
        }

        public void Dispose()
        {
            _view.ClickedProjectType -= _eventManager.Remove<ProjectConfigTypes>(OnClickedProjectType);

            _view.ClickedProjectApplyButton -= _eventManager.Remove(OnClickedProjectApplyButton);
            _view.ClickedProjectRevertButton -= _eventManager.Remove(OnClickedProjectRevertButton);
            _view.ClickedProjectSaveButton -= _eventManager.Remove(OnClickedProjectSaveButton);
            _view.ClickedProjectEditButton -= _eventManager.Remove(OnClickedProjectEditButton);

            _model.PluginItemsChanged -= _eventManager.Remove(OnProjectConfigDirty);
            _model.AssetItemsChanged -= _eventManager.Remove(OnProjectConfigDirty);
            _model.PackagesChanged -= _eventManager.Remove(OnProjectConfigDirty);
            _model.ReleasesChanged -= _eventManager.Remove(OnProjectConfigDirty);
            _model.VsProjectsChanged -= _eventManager.Remove(OnProjectConfigDirty);

            _projectHandler.SavedConfigFile -= _eventManager.Remove(OnSavedConfigFile);
            _projectHandler.LoadedConfigFile -= _eventManager.Remove(OnLoadedConfigFile);

            _view.RemoveContextMenuHandler(DragListTypes.AssetItem);
            _view.RemoveContextMenuHandler(DragListTypes.PluginItem);

            _eventManager.AssertIsEmpty();
        }

        void OnLoadedConfigFile()
        {
            _view.IsSaveEnabled = false;
        }

        void OnSavedConfigFile()
        {
            _view.IsSaveEnabled = false;
        }

        void OnProjectConfigDirty()
        {
            _view.IsSaveEnabled = true;
        }

        IEnumerable<ContextMenuItem> GetProjectItemContextMenu()
        {
            var selected = GetSelectedItems();

            yield return new ContextMenuItem(
                !selected.IsEmpty(), "Remove", false, OnContextMenuDeleteSelected);

            yield return new ContextMenuItem(
                selected.Count == 1 && HasFolderWithPackageName(selected.Single()), "Select in Project Tab", false, OnContextMenuShowSelectedInProjectTab);
        }

        List<string> GetSelectedItems()
        {
            return _view.GetSelected(DragListTypes.AssetItem).Concat(_view.GetSelected(DragListTypes.PluginItem))
                .Select(x => (string)x.Model).ToList();
        }

        bool HasFolderWithPackageName(string name)
        {
            return TryGetAssetForPackageName(name) != null;
        }

        UnityEngine.Object TryGetAssetForPackageName(string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/" + name);

            if (asset == null)
            {
                asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Plugins/" + name);
            }

            return asset;
        }

        void OnContextMenuShowSelectedInProjectTab()
        {
            var selected = GetSelectedItems();

            Assert.IsEqual(selected.Count, 1);

            var name = selected.Single();

            var asset = TryGetAssetForPackageName(name);

            if (asset == null)
            {
                _errorHandler.DisplayError(
                    "Could not find package '{0}' in project".Fmt(name));
            }
            else
            {
                Selection.activeObject = asset;
            }
        }

        void OnContextMenuDeleteSelected()
        {
            var selected = GetSelectedItems();

            foreach (var name in selected)
            {
                if (_model.HasAssetItem(name))
                {
                    _model.RemoveAssetItem(name);
                }
                else
                {
                    Assert.That(_model.HasPluginItem(name));
                    _model.RemovePluginItem(name);
                }
            }
        }

        public void Update()
        {
            _eventManager.Flush();

            var configFileExists = File.Exists(ProjenyEditorUtil.GetProjectConfigPath(_view.ProjectConfigType));

            if (configFileExists)
            {
                _view.IsRevertEnabled = true;
            }
            else
            {
                _view.IsSaveEnabled = true;
                _view.IsRevertEnabled = false;
            }

            _view.IsEditEnabled = configFileExists;
        }

        public void OnClickedProjectType(ProjectConfigTypes desiredConfigType)
        {
            _asyncProcessor.Process(
                TryChangeProjectType(desiredConfigType));
        }

        IEnumerator TryChangeProjectType(ProjectConfigTypes configType)
        {
            if (_projectHandler.HasProjectConfigChanged())
            {
                var fileName = Path.GetFileName(ProjenyEditorUtil.GetProjectConfigPath(_view.ProjectConfigType));
                var choice = _view.PromptForUserChoice(
                    "Do you want to save changes to {0}?".Fmt(fileName), new[] { "Save", "Don't Save", "Cancel" }, null, null, 0, 2);

                yield return choice;

                switch (choice.Current)
                {
                    case 0:
                    {
                        _projectHandler.OverwriteConfig();
                        break;
                    }
                    case 1:
                    {
                        // Do nothing
                        break;
                    }
                    case 2:
                    {
                        yield break;
                    }
                    default:
                    {
                        Assert.Throw();
                        break;
                    }
                }
            }

            _view.ProjectConfigType = configType;
            _projectHandler.RefreshProject();
        }

        public void OnClickedProjectEditButton()
        {
            var configPath = ProjenyEditorUtil.GetProjectConfigPath(_view.ProjectConfigType);
            InternalEditorUtility.OpenFileAtLineExternal(configPath, 1);
        }

        public void OnClickedProjectSaveButton()
        {
            _projectHandler.OverwriteConfig();
        }

        public void OnClickedProjectRevertButton()
        {
            _projectHandler.RefreshProject();
        }

        public void OnClickedProjectApplyButton()
        {
            _asyncProcessor.Process(ApplyProjectChangeAsync(), "Updating Links");
        }

        IEnumerator ApplyProjectChangeAsync()
        {
            _projectHandler.OverwriteConfig();

            yield return _prjCommandHandler.ProcessPrjCommand(
                "Updating directory links", PrjHelper.UpdateLinksAsync());

            yield return _prjCommandHandler.ProcessPrjCommand(
                "Updating custom solution", PrjHelper.UpdateCustomSolutionAsync());
        }
    }
}

