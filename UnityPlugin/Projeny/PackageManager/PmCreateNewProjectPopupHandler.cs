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
    public class PmCreateNewProjectPopupHandler
    {
        readonly PrjCommandHandler _commandHandler;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmView _view;

        public PmCreateNewProjectPopupHandler(
            PmView view,
            AsyncProcessor asyncProcessor,
            PrjCommandHandler commandHandler)
        {
            _commandHandler = commandHandler;
            _asyncProcessor = asyncProcessor;
            _view = view;
        }

        public void ShowCreateNewProjectPopup()
        {
            _asyncProcessor.Process(ShowCreateNewProjectPopupAsync(), "Creating Project");
        }

        IEnumerator ShowCreateNewProjectPopupAsync()
        {
            var userInput = _view.PromptForInput("Enter new project name:", "Untitled");

            yield return userInput;

            if (userInput.Current == null)
            {
                // User Cancelled
                yield break;
            }

            var projName = userInput.Current;

            yield return _commandHandler.ProcessPrjCommand(
                "Creating Project '{0}'".Fmt(projName), PrjHelper.CreateProjectAsync(projName));

            yield return _commandHandler.ProcessPrjCommand(
                "Initializing directory links", PrjHelper.UpdateLinksAsyncForProject(projName));

            yield return _commandHandler.ProcessPrjCommand(
                "Opening Unity", PrjHelper.OpenUnityForProjectAsync(projName));

            EditorApplication.Exit(0);
        }
    }
}



