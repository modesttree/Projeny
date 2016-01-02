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
    public class PmController : IDisposable
    {
        readonly PmModel _model;
        readonly PmView.Model _viewModel;

        PmView _view;

        PmModelViewSyncer _viewModelSyncer;

        EventManager _eventManager;

        AsyncProcessor _asyncProcessor;

        PmProjectViewHandler _projectViewHandler;
        PmProjectHandler _projectHandler;

        PmReleasesHandler _releasesHandler;
        PmReleasesViewHandler _releasesViewHandler;

        UpmCommandHandler _upmCommandHandler;
        PmViewAsyncHandler _viewAsyncHandler;
        PmViewErrorHandler _viewErrorHandler;
        PmPackageViewHandler _packageViewHandler;
        PmPackageHandler _packageHandler;

        public PmController(PmModel model, PmView.Model viewModel)
        {
            _model = model;
            _viewModel = viewModel;
        }

        public void Initialize()
        {
            SetupDependencies();
            Start();
        }

        void Start()
        {
            _viewModelSyncer.Initialize();
            _projectViewHandler.Initialize();
            _packageViewHandler.Initialize();
            _releasesViewHandler.Initialize();

            ObserveViewEvents();
        }

        void SetupDependencies()
        {
            // We could use a DI framework like zenject here but it's overkill
            // and also we'd like to keep the dependencies for Projeny low
            // So just do poor man's DI instead
            _asyncProcessor = new AsyncProcessor();
            _eventManager = new EventManager();

            _view = new PmView(_viewModel);

            _upmCommandHandler = new UpmCommandHandler(_view);

            _viewAsyncHandler = new PmViewAsyncHandler(_view, _asyncProcessor);
            _viewModelSyncer = new PmModelViewSyncer(_model, _view);
            _projectHandler = new PmProjectHandler(_model, _view);
            _releasesHandler = new PmReleasesHandler(_model, _upmCommandHandler);
            _viewErrorHandler = new PmViewErrorHandler(_view, _asyncProcessor);
            _packageHandler = new PmPackageHandler(_model, _upmCommandHandler);
            _packageViewHandler = new PmPackageViewHandler(_view, _asyncProcessor, _packageHandler);

            _projectViewHandler = new PmProjectViewHandler(
                _model, _view, _projectHandler, _asyncProcessor,
                _upmCommandHandler, _viewErrorHandler);

            _releasesViewHandler = new PmReleasesViewHandler(_model, _view, _asyncProcessor, _releasesHandler);
        }

        void ObserveViewEvents()
        {
            // Use EventQueueMode.LatestOnly to ensure we don't execute anything during the OnGUI event
            // This is important since OnGUI is called in multiple passes and we have to ensure that the same
            // controls are rendered each pass
            _view.ClickedRefreshPackages += _eventManager.Add(OnClickedRefreshPackages, EventQueueMode.LatestOnly);
            _view.ClickedCreateNewPackage += _eventManager.Add(OnClickedCreateNewPackage, EventQueueMode.LatestOnly);

            _view.ClickedReleasesSortMenu += _eventManager.Add<Rect>(OnClickedReleasesSortMenu, EventQueueMode.LatestOnly);

            _view.DraggedDroppedListEntries += _eventManager.Add<ListTypes, ListTypes, List<DraggableListEntry>>(OnDraggedDroppedListEntries, EventQueueMode.LatestOnly);
        }

        void UnobserveViewEvents()
        {
            _view.ClickedRefreshPackages -= _eventManager.Remove(OnClickedRefreshPackages);
            _view.ClickedCreateNewPackage -= _eventManager.Remove(OnClickedCreateNewPackage);

            _view.ClickedReleasesSortMenu -= _eventManager.Remove<Rect>(OnClickedReleasesSortMenu);

            _view.DraggedDroppedListEntries -= _eventManager.Remove<ListTypes, ListTypes, List<DraggableListEntry>>(OnDraggedDroppedListEntries);

            _eventManager.AssertIsEmpty();
        }

        void OnDraggedDroppedListEntries(ListTypes sourceType, ListTypes dropType, List<DraggableListEntry> entries)
        {
            foreach (var entry in entries)
            {
                OnDraggedDroppedListEntry(sourceType, dropType, entry);
            }
        }

        void OnDraggedDroppedListEntry(ListTypes sourceType, ListTypes dropType, DraggableListEntry entry)
        {
            switch (dropType)
            {
                case ListTypes.Package:
                {
                    switch (sourceType)
                    {
                        case ListTypes.PluginItem:
                        {
                            var name = (string)entry.Model;
                            _model.RemovePluginItem(name);
                            break;
                        }
                        case ListTypes.AssetItem:
                        {
                            var name = (string)entry.Model;
                            _model.RemoveAssetItem(name);
                            break;
                        }
                        case ListTypes.Release:
                        {
                            Assert.Throw("TODO");
                            //AddBackgroundTask(InstallReleasesAsync(data.Entries.Select(x => (ReleaseInfo)x.Tag).ToList()), "Installing Releases");
                            break;
                        }
                        default:
                        {
                            Assert.Throw();
                            break;
                        }
                    }

                    break;
                }
                case ListTypes.PluginItem:
                {
                    switch (sourceType)
                    {
                        case ListTypes.AssetItem:
                        {
                            var name = (string)entry.Model;
                            _model.RemoveAssetItem(name);
                            _model.AddPluginItem(name);

                            break;
                        }
                        case ListTypes.PluginItem:
                        {
                            // Do nothing
                            break;
                        }
                        case ListTypes.Package:
                        {
                            var info = (PackageInfo)entry.Model;

                            if (!_model.HasPluginItem(info.Name))
                            {
                                if (_model.HasAssetItem(info.Name))
                                {
                                    _model.RemoveAssetItem(info.Name);
                                }

                                _model.AddPluginItem(info.Name);
                            }

                            break;
                        }
                        default:
                        {
                            Assert.Throw("TODO");
                            break;
                        }
                    }

                    break;
                }
                case ListTypes.AssetItem:
                {
                    switch (sourceType)
                    {
                        case ListTypes.AssetItem:
                        {
                            // Do nothing
                            break;
                        }
                        case ListTypes.PluginItem:
                        {
                            var name = (string)entry.Model;

                            _model.RemovePluginItem(name);
                            _model.AddAssetItem(name);
                            break;
                        }
                        case ListTypes.Package:
                        {
                            var info = (PackageInfo)entry.Model;

                            if (!_model.HasAssetItem(info.Name))
                            {
                                if (_model.HasPluginItem(info.Name))
                                {
                                    _model.RemovePluginItem(info.Name);
                                }

                                _model.AddAssetItem(info.Name);
                            }

                            break;
                        }
                        default:
                        {
                            Assert.Throw();
                            break;
                        }
                    }

                    break;
                }
                case ListTypes.Release:
                {
                    // Nothing can drag here
                    break;
                }
                default:
                {
                    Assert.Throw();
                    break;
                }
            }
        }

        public void Dispose()
        {
            UnobserveViewEvents();

            _viewModelSyncer.Dispose();
            _releasesViewHandler.Dispose();
        }

        public void OnClickedReleasesSortMenu(Rect buttonRect)
        {
            ShowReleasesSortMenu(new Vector2(buttonRect.xMin, buttonRect.yMax));
        }

        void ShowReleasesSortMenu(Vector2 startPos)
        {
            GenericMenu contextMenu = new GenericMenu();

            contextMenu.AddItem(
                new GUIContent("Order By Name"),
                _view.ReleasesSortMethod == ReleasesSortMethod.Name,
                () => ChangeReleaseSortMethod(ReleasesSortMethod.Name));

            contextMenu.AddItem(
                new GUIContent("Order By Size"),
                _view.ReleasesSortMethod == ReleasesSortMethod.Size,
                () => ChangeReleaseSortMethod(ReleasesSortMethod.Size));

            contextMenu.AddItem(
                new GUIContent("Order By Publish Date"),
                _view.ReleasesSortMethod == ReleasesSortMethod.PublishDate,
                () => ChangeReleaseSortMethod(ReleasesSortMethod.PublishDate));

            contextMenu.DropDown(new Rect(startPos.x, startPos.y, 0, 0));
        }

        void ChangeReleaseSortMethod(ReleasesSortMethod sortMethod)
        {
            _view.ReleasesSortMethod = sortMethod;
            Assert.Throw("TODO");
            //_releasesList.ForceSort();
        }

        public void OnClickedCreateNewPackage()
        {
            _asyncProcessor.Process(CreateNewPackageAsync());
        }

        public void OnClickedRefreshPackages()
        {
            _asyncProcessor.Process(_packageHandler.RefreshPackagesAsync(), "Refreshing Packages");
        }

        IEnumerator RefreshAll()
        {
            _projectHandler.RefreshProject();
            yield return _packageHandler.RefreshPackagesAsync();
            yield return _releasesHandler.RefreshReleasesAsync();
        }

        public void Update()
        {
            _eventManager.Flush();
            _asyncProcessor.Tick();

            _projectViewHandler.Update();
            _packageViewHandler.Update();

            // Do this last so all model updates get forwarded to view
            _viewModelSyncer.Update();
            _viewAsyncHandler.Update();
            _releasesViewHandler.Update();

            _view.Update();

            //UpdateBackgroundTask();

            //if (_backgroundTaskInfo != null)
            //{
            //_view.IsBlocked = true;
            //_view.BlockedStatusMessage = _backgroundTaskInfo.StatusMessage;
            //_view.BlockedStatusTitle = _backgroundTaskInfo.StatusTitle;
            //}
            //else
            //{
            //_view.IsBlocked = false;
            //_view.BlockedStatusMessage = null;
            //_view.BlockedStatusTitle = null;
            //}
        }

        //void UpdateBackgroundTask()
        //{
        //if (_backgroundTaskInfo == null)
        //{
        //return;
        //}

        //try
        //{
        //// NOTE: Do not assume a constant frame rate here
        //// (When we aren't in focus this gets updated less often)
        //if (!_backgroundTaskInfo.CoRoutine.Pump())
        //{
        //_backgroundTaskInfo = null;
        //}
        //}
        //catch (CoRoutineException e)
        //{
        //_backgroundTaskInfo = null;

        //// If possible, display this as a popup
        //// Otherwise it will still be visible in the console so that's fine
        //if (e.InnerException != null)
        //{
        //AddBackgroundTask(
            //_view.DisplayError(e.InnerException.Message));
            //}

            //throw;
            //}
            //catch (Exception e)
            //{
            //_backgroundTaskInfo = null;

            //AddBackgroundTask(
                //_view.DisplayError(e.Message));
                //throw;
                //}
                //}

        public void OnContextMenuOpened(DraggableList sourceList)
        {
            // Move this to view?

            //var listType = ClassifyList(sourceList);

            //GenericMenu contextMenu = new GenericMenu();

            //switch (listType)
            //{
                //case ListTypes.Release:
                //{
                    //bool hasLocalPath = false;
                    //bool hasAssetStoreLink = false;

                    //var singleInfo = _selected.OnlyOrDefault();

                    //if (singleInfo != null)
                    //{
                        //var info = (ReleaseInfo)singleInfo.Tag;

                        //hasLocalPath = info.LocalPath != null && File.Exists(info.LocalPath);

                        //hasAssetStoreLink = info.AssetStoreInfo != null && !string.IsNullOrEmpty(info.AssetStoreInfo.LinkId);
                    //}

                    //contextMenu.AddOptionalItem(hasLocalPath, new GUIContent("Open Folder"), false, OpenReleaseFolderForSelected);

                    //contextMenu.AddOptionalItem(singleInfo != null, new GUIContent("More Info..."), false, OpenMoreInfoPopupForSelected);

                    //contextMenu.AddOptionalItem(hasAssetStoreLink, new GUIContent("Open In Asset Store"), false, OpenSelectedInAssetStore);
                    //break;
                //}
                //case ListTypes.Package:
                //{
                    //break;
                //}
                //case ListTypes.AssetItem:
                //case ListTypes.PluginItem:
                //{
                    //contextMenu.AddItem(new GUIContent("Remove"), false, DeleteSelected);
                    //contextMenu.AddOptionalItem(_selected.Count == 1 && HasFolderWithPackageName(_selected.Single().Name), new GUIContent("Select in Project Tab"), false, ShowSelectedInProjectTab);
                    //break;
                //}
                //default:
                //{
                    //Assert.Throw();
                    //break;
                //}
            //}

            //contextMenu.ShowAsContext();
        }

        void OpenSelectedInAssetStore()
        {
            Assert.Throw("TODO");
            //Assert.IsEqual(_selected.Count, 1);

            //var entry = _selected.Single();

            //Assert.IsEqual(ClassifyList(entry.ListOwner), ListTypes.Release);

            //var info = (ReleaseInfo)entry.Tag;
            //var assetStoreInfo = info.AssetStoreInfo;

            //Assert.IsNotNull(assetStoreInfo);

            //var fullUrl = "https://www.assetstore.unity3d.com/#/{0}/{1}".Fmt(assetStoreInfo.LinkType, assetStoreInfo.LinkId);
            //Application.OpenURL(fullUrl);
        }

        void OpenMoreInfoPopupForSelected()
        {
            Assert.Throw("TODO");
            //Assert.IsEqual(_selected.Count, 1);

            //var entry = _selected.Single();

            //switch (ClassifyList(entry.ListOwner))
            //{
            //case ListTypes.Package:
            //{
            //AddBackgroundTask(OpenMoreInfoPopup((PackageInfo)entry.Tag));
            //break;
            //}
            //case ListTypes.Release:
            //{
            //AddBackgroundTask(OpenMoreInfoPopup((ReleaseInfo)entry.Tag));
            //break;
            //}
            //default:
            //{
            //Assert.Throw();
            //break;
            //}
            //}
        }

        IEnumerator OpenMoreInfoPopup(PackageInfo info)
        {
            Assert.Throw("TODO");
            yield break;
        }

        void OpenReleaseFolderForSelected()
        {
            Assert.Throw("TODO");
            //Assert.IsEqual(_selected.Count, 1);

            //var info = (ReleaseInfo)_selected.Single().Tag;

            //Assert.IsNotNull(info.LocalPath);
            //PathUtil.AssertPathIsValid(info.LocalPath);

            //var args = @"/select, " + info.LocalPath;
            //System.Diagnostics.Process.Start("explorer.exe", args);
        }

        void EditPackageYamlSelected()
        {
            Assert.Throw("TODO");
            //Assert.IsEqual(_selected.Count, 1);

            //var info = (PackageInfo)_selected.Single().Tag;

            //var configPath = Path.Combine(info.Path, ProjenyEditorUtil.PackageConfigFileName);

            //Assert.That(File.Exists(configPath));

            //InternalEditorUtility.OpenFileAtLineExternal(configPath, 1);
        }

        void OpenPackageFolderForSelected()
        {
            Assert.Throw("TODO");
            //Assert.IsEqual(_selected.Count, 1);

            //var info = (PackageInfo)_selected.Single().Tag;

            //Assert.That(Directory.Exists(info.Path));

            //System.Diagnostics.Process.Start(info.Path);
        }

        void SelectAll()
        {
            Assert.Throw("TODO");
            //if (_selected.IsEmpty())
            //{
            //return;
            //}

            //var listType = ClassifyList(_selected[0].ListOwner);

            //Assert.That(_selected.All(x => ClassifyList(x.ListOwner) == listType));

            //foreach (var entry in _selected[0].ListOwner.Values)
            //{
            //SelectInternal(entry);
            //}
        }

        void DeleteSelected()
        {
        }

        IEnumerator DeleteSelectedAsync()
        {
            Assert.Throw("TODO");
            yield break;
            //if (_selected.IsEmpty())
            //{
                //yield break;
            //}

            //var listType = ClassifyList(_selected[0].ListOwner);

            //Assert.That(_selected.All(x => ClassifyList(x.ListOwner) == listType));

            //if (listType == ListTypes.Package)
            //{
            //}
            //else if (listType == ListTypes.AssetItem || listType == ListTypes.PluginItem)
            //{
                //var entriesToRemove = _selected.ToList();
                //_selected.Clear();

                //foreach (var entry in entriesToRemove)
                //{
                    //entry.ListOwner.Remove(entry);
                //}
            //}
        }

        IEnumerator InstallReleasesAsync(List<ReleaseInfo> releaseInfos)
        {
            // Need to make sure we have the most recent package list so we can determine whether this is
            // an upgrade / downgrade / etc.
            yield return _packageHandler.RefreshPackagesAsync();

            Assert.That(releaseInfos.Select(x => x.Id).GetDuplicates().IsEmpty(), "Found duplicate releases selected - are you installing multiple versions of the same release?");

            foreach (var releaseInfo in releaseInfos)
            {
                var userChoice = CheckShouldInstall(releaseInfo);

                yield return userChoice;

                switch (userChoice.Current)
                {
                    case InstallReleaseUserChoices.Cancel:
                    {
                        yield break;
                    }
                    case InstallReleaseUserChoices.Install:
                    {
                        yield return _upmCommandHandler.ProcessUpmCommand(
                            "Installing release '{0}'".Fmt(releaseInfo.Name), UpmHelper.InstallReleaseAsync(releaseInfo));
                        break;
                    }
                    case InstallReleaseUserChoices.Skip:
                    {
                        // Do nothing
                        break;
                    }
                    default:
                    {
                        Assert.Throw();
                        break;
                    }
                }
            }

            yield return _packageHandler.RefreshPackagesAsync();
        }

        IEnumerator<InstallReleaseUserChoices> CheckShouldInstall(ReleaseInfo releaseInfo)
        {
            return CoRoutine.Wrap<InstallReleaseUserChoices>(CheckShouldInstallInternal(releaseInfo));
        }

        IEnumerator CheckShouldInstallInternal(ReleaseInfo releaseInfo)
        {
            var packageInfo = TryFindPackageInfoForRelease(releaseInfo);

            if (packageInfo == null)
            {
                yield return InstallReleaseUserChoices.Install;
                yield break;
            }

            Assert.IsNotNull(packageInfo.InstallInfo);
            var packageReleaseInfo = packageInfo.InstallInfo.ReleaseInfo;

            Assert.IsNotNull(packageReleaseInfo);

            // TODO - how to handle?
            Assert.That(packageReleaseInfo.HasVersionCode);
            Assert.That(releaseInfo.HasVersionCode);

            IEnumerator<int> userChoice;

            if (packageReleaseInfo.VersionCode == releaseInfo.VersionCode)
            {
                Assert.IsEqual(releaseInfo.Version, packageReleaseInfo.Version);

                userChoice = _view.PromptForUserChoice(
                    "Package '{0}' is already installed with the same version ('{1}').  Would you like to re-install it anyway?  Note that any local changes you've made to the package will be reverted."
                    .Fmt(packageReleaseInfo.Name, packageReleaseInfo.Version), new[] { "Overwrite", "Skip", "Cancel" });
            }
            else if (releaseInfo.VersionCode > packageReleaseInfo.VersionCode)
            {
                userChoice = _view.PromptForUserChoice(
                    "Package '{0}' is already installed with version '{1}'. Would you like to UPGRADE it to version '{2}'?  Note that any local changes you've made to the package will be lost."
                    .Fmt(releaseInfo.Name, packageReleaseInfo.Version, releaseInfo.Version), new[] { "Upgrade", "Skip", "Cancel" });
            }
            else
            {
                Assert.That(releaseInfo.VersionCode < packageReleaseInfo.VersionCode);

                userChoice = _view.PromptForUserChoice(
                    "Package '{0}' is already installed with version '{1}'. Would you like to DOWNGRADE it to version '{2}'?  Note that any local changes you've made to the package will be lost."
                    .Fmt(releaseInfo.Name, packageReleaseInfo.Version, releaseInfo.Version), new[] { "Downgrade", "Skip", "Cancel" });
            }

            yield return userChoice;

            switch (userChoice.Current)
            {
                case 0:
                {
                    yield return InstallReleaseUserChoices.Install;
                    break;
                }
                case 1:
                {
                    yield return InstallReleaseUserChoices.Skip;
                    break;
                }
                case 2:
                {
                    yield return InstallReleaseUserChoices.Cancel;
                    break;
                }
                default:
                {
                    Assert.Throw();
                    break;
                }
            }
        }

        PackageInfo TryFindPackageInfoForRelease(ReleaseInfo releaseInfo)
        {
            foreach (var packageInfo in _model.Packages)
            {
                if (packageInfo.InstallInfo != null && packageInfo.InstallInfo.ReleaseInfo != null && packageInfo.InstallInfo.ReleaseInfo.Id == releaseInfo.Id)
                {
                    return packageInfo;
                }
            }

            return null;
        }

        ListTypes ClassifyList(DraggableList list)
        {
            Assert.Throw("TODO");
            return ListTypes.AssetItem;
        }

        void ForceStopBackgroundTask()
        {
            Assert.Throw("TODO");
            //_backgroundTaskInfo = null;
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

            yield return _upmCommandHandler.ProcessUpmCommand("Creating Package '{0}'".Fmt(userInput.Current), UpmHelper.CreatePackageAsync(userInput.Current));
            yield return _packageHandler.RefreshPackagesAsync();
        }

        public void OnGUI(Rect fullRect)
        {
            _view.OnGUI(fullRect);

            CheckForKeypresses();
        }

        void CheckForKeypresses()
        {
            if (!GUI.enabled)
            {
                // Popup visible
                return;
            }

            var e = Event.current;

            if (e.type == EventType.ValidateCommand)
            {
                if (e.commandName == "SelectAll")
                {
                    e.Use();
                }
            }
            else if (e.type == EventType.ExecuteCommand)
            {
                if (e.commandName == "SelectAll")
                {
                    SelectAll();
                    e.Use();
                }
            }
            else if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.Delete:
                    {
                        DeleteSelected();
                        e.Use();
                        break;
                    }
                }
            }
        }

        public class BackgroundTaskInfo
        {
            public CoRoutine CoRoutine;

            public string ProcessName;
            public string StatusTitle;
            public string StatusMessage;
        }

        enum InstallReleaseUserChoices
        {
            Install,
            Cancel,
            Skip,
        }
    }
}

