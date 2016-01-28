using System;
using System.IO;
using ModestTree;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Projeny.Internal;
using System.Linq;
using ModestTree.Util;

namespace Projeny.Internal
{
    public class PmCreateNewProjectPopupHandler
    {
        readonly PmWindowInitializer _windowInitializer;
        readonly PrjCommandHandler _commandHandler;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmView _view;

        public PmCreateNewProjectPopupHandler(
            PmView view,
            AsyncProcessor asyncProcessor,
            PrjCommandHandler commandHandler,
            PmWindowInitializer windowInitializer)
        {
            _windowInitializer = windowInitializer;
            _commandHandler = commandHandler;
            _asyncProcessor = asyncProcessor;
            _view = view;
        }

        public void ShowCreateNewProjectPopup()
        {
            _asyncProcessor.Process(ShowCreateNewProjectPopupAsync(), true, "Creating Project");
        }

        IEnumerator ShowCreateNewProjectPopupAsync()
        {
            while (!_windowInitializer.IsInitialized)
            {
                yield return null;
            }

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


