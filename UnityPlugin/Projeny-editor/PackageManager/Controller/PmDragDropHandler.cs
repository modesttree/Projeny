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
    public class PmDragDropHandler
    {
        readonly UpmCommandHandler _upmCommandHandler;
        readonly PmPackageHandler _packageHandler;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmView _view;
        readonly PmModel _model;

        readonly EventManager _eventManager = new EventManager();

        public PmDragDropHandler(
            PmModel model,
            PmView view,
            AsyncProcessor asyncProcessor,
            PmPackageHandler packageHandler,
            UpmCommandHandler upmCommandHandler)
        {
            _upmCommandHandler = upmCommandHandler;
            _packageHandler = packageHandler;
            _asyncProcessor = asyncProcessor;
            _view = view;
            _model = model;
        }

        public void Initialize()
        {
            _view.DraggedDroppedListEntries += _eventManager.Add<ListTypes, ListTypes, List<DraggableListEntry>>(OnDraggedDroppedListEntries, EventQueueMode.LatestOnly);
        }

        public void Dispose()
        {
            _view.DraggedDroppedListEntries -= _eventManager.Remove<ListTypes, ListTypes, List<DraggableListEntry>>(OnDraggedDroppedListEntries);
            _eventManager.AssertIsEmpty();
        }

        public void Update()
        {
            _eventManager.Flush();
        }

        void OnDraggedDroppedListEntries(ListTypes sourceType, ListTypes dropType, List<DraggableListEntry> entries)
        {
            switch (dropType)
            {
                case ListTypes.Package:
                {
                    switch (sourceType)
                    {
                        case ListTypes.PluginItem:
                        {
                            foreach (var entry in entries)
                            {
                                var name = (string)entry.Model;
                                _model.RemovePluginItem(name);
                            }
                            break;
                        }
                        case ListTypes.AssetItem:
                        {
                            foreach (var entry in entries)
                            {
                                var name = (string)entry.Model;
                                _model.RemoveAssetItem(name);
                            }
                            break;
                        }
                        case ListTypes.Release:
                        {
                            _asyncProcessor.Process(
                                InstallReleasesAsync(
                                    entries.Select(x => (ReleaseInfo)x.Model).ToList()), "Installing Releases");
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
                            foreach (var entry in entries)
                            {
                                var name = (string)entry.Model;
                                _model.RemoveAssetItem(name);
                                _model.AddPluginItem(name);
                            }

                            break;
                        }
                        case ListTypes.PluginItem:
                        {
                            // Do nothing
                            break;
                        }
                        case ListTypes.Package:
                        {
                            foreach (var entry in entries)
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
                            foreach (var entry in entries)
                            {
                                var name = (string)entry.Model;

                                _model.RemovePluginItem(name);
                                _model.AddAssetItem(name);
                            }

                            break;
                        }
                        case ListTypes.Package:
                        {
                            foreach (var entry in entries)
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

        IEnumerator<InstallReleaseUserChoices> CheckShouldInstall(ReleaseInfo releaseInfo)
        {
            return CoRoutine.Wrap<InstallReleaseUserChoices>(CheckShouldInstallInternal(releaseInfo));
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
                    .Fmt(packageReleaseInfo.Name, packageReleaseInfo.Version), new[] { "Overwrite", "Skip", "Cancel" }, null, null, 0, 2);
            }
            else if (releaseInfo.VersionCode > packageReleaseInfo.VersionCode)
            {
                userChoice = _view.PromptForUserChoice(
                    "Package '{0}' is already installed with version '{1}'. Would you like to UPGRADE it to version '{2}'?  Note that any local changes you've made to the package will be lost."
                    .Fmt(releaseInfo.Name, packageReleaseInfo.Version, releaseInfo.Version), new[] { "Upgrade", "Skip", "Cancel" }, null, null, 0, 2);
            }
            else
            {
                Assert.That(releaseInfo.VersionCode < packageReleaseInfo.VersionCode);

                userChoice = _view.PromptForUserChoice(
                    "Package '{0}' is already installed with version '{1}'. Would you like to DOWNGRADE it to version '{2}'?  Note that any local changes you've made to the package will be lost."
                    .Fmt(releaseInfo.Name, packageReleaseInfo.Version, releaseInfo.Version), new[] { "Downgrade", "Skip", "Cancel" }, null, null, 0, 2);
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

        public IEnumerator InstallReleasesAsync(List<ReleaseInfo> releaseInfos)
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

        enum InstallReleaseUserChoices
        {
            Install,
            Cancel,
            Skip,
        }
    }
}
