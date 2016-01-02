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
    public class PmProjectViewHandler
    {
        readonly UpmCommandHandler _upmCommandHandler;
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
            UpmCommandHandler upmCommandHandler)
        {
            _upmCommandHandler = upmCommandHandler;
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
        }

        public void Dispose()
        {
            _view.ClickedProjectType -= _eventManager.Remove<ProjectConfigTypes>(OnClickedProjectType);

            _view.ClickedProjectApplyButton -= _eventManager.Remove(OnClickedProjectApplyButton);
            _view.ClickedProjectRevertButton -= _eventManager.Remove(OnClickedProjectRevertButton);
            _view.ClickedProjectSaveButton -= _eventManager.Remove(OnClickedProjectSaveButton);
            _view.ClickedProjectEditButton -= _eventManager.Remove(OnClickedProjectEditButton);

            _eventManager.AssertIsEmpty();
        }

        public void Update()
        {
            _eventManager.Flush();
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
                var choice = _view.PromptForUserChoice(
                    "Do you want to save changes to your project?", new[] { "Save", "Don't Save", "Cancel" });

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

            _model.ProjectConfigType = configType;
            _projectHandler.RefreshProject();
        }

        public void OnClickedProjectEditButton()
        {
            var configPath = ProjenyEditorUtil.GetProjectConfigPath(_model.ProjectConfigType);
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
            _projectHandler.OverwriteConfig();

            _asyncProcessor.Process(
                _upmCommandHandler.ProcessUpmCommand(
                    "Updating directory links", UpmHelper.UpdateLinksAsync()), "Updating Links");
        }
    }
}

