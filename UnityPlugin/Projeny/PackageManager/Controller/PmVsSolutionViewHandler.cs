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
    public class PmVsSolutionViewHandler
    {
        readonly PmProjectHandler _projectHandler;
        readonly PrjCommandHandler _prjCommandHandler;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmModel _model;
        readonly PmView _view;

        readonly EventManager _eventManager = new EventManager(null);

        public PmVsSolutionViewHandler(
            PmModel model,
            PmView view,
            AsyncProcessor asyncProcessor,
            PrjCommandHandler prjCommandHandler,
            PmProjectHandler projectHandler)
        {
            _projectHandler = projectHandler;
            _prjCommandHandler = prjCommandHandler;
            _asyncProcessor = asyncProcessor;
            _model = model;
            _view = view;
        }

        public void Initialize()
        {
            _view.AddContextMenuHandler(DragListTypes.VsSolution, GetVsSolutionItemContextMenu);

            _view.ClickedUpdateSolution += _eventManager.Add(OnClickedUpdateSolution, EventQueueMode.LatestOnly);
            _view.ClickedOpenSolution += _eventManager.Add(OnClickedOpenSolution, EventQueueMode.LatestOnly);
        }

        public void Dispose()
        {
            _view.RemoveContextMenuHandler(DragListTypes.VsSolution);

            _view.ClickedUpdateSolution -= _eventManager.Remove(OnClickedUpdateSolution);
            _view.ClickedOpenSolution -= _eventManager.Remove(OnClickedOpenSolution);

            _eventManager.AssertIsEmpty();
        }

        public void Update()
        {
            _eventManager.Flush();
        }

        List<string> GetSelectedItems()
        {
            return _view.GetSelected(DragListTypes.VsSolution)
                .Select(x => (string)x.Model).ToList();
        }

        IEnumerable<ContextMenuItem> GetVsSolutionItemContextMenu()
        {
            var selected = GetSelectedItems();

            if (selected.IsEmpty())
            {
                yield return new ContextMenuItem(
                    selected.IsEmpty(), "Add As Regex...", false, OnContextMenuAddAsRegex);
            }
            else
            {
                yield return new ContextMenuItem(
                    !selected.IsEmpty(), "Remove", false, OnContextMenuDeleteSelected);
            }
        }

        void OnContextMenuAddAsRegex()
        {
            _asyncProcessor.Process(AddAsRegexAsync());
        }

        IEnumerator AddAsRegexAsync()
        {
            var userInput = _view.PromptForInput("Enter Python Regex below.\n (note: see python documentation for reference)", ".*");

            yield return userInput;

            if (userInput.Current == null)
            {
                // User Cancelled
                yield break;
            }

            Assert.That(!userInput.Current.StartsWith("/"), "When entering the regex, you do not need to prefix it with a slash");

            _model.AddVsProject("/" + userInput.Current);
        }

        void OnContextMenuDeleteSelected()
        {
            foreach (var name in GetSelectedItems())
            {
                _model.RemoveVsProject(name);
            }
        }

        public void OnClickedOpenSolution()
        {
            _asyncProcessor.Process(
                OpenCustomSolutionAsync(), true, "Opening Visual Studio Solution");
        }

        IEnumerator OpenCustomSolutionAsync()
        {
            yield return UpdateCustomSolutionAsync();

            yield return _prjCommandHandler.ProcessPrjCommand(
                "Opening solution", PrjHelper.OpenCustomSolutionAsync());
        }

        public void OnClickedUpdateSolution()
        {
            _asyncProcessor.Process(
                UpdateCustomSolutionAsync(), true, "Updating Visual Studio Solution");
        }

        IEnumerator UpdateCustomSolutionAsync()
        {
            _projectHandler.OverwriteConfig();

            return _prjCommandHandler.ProcessPrjCommand(
                "Updating solution", PrjHelper.UpdateCustomSolutionAsync());
        }
    }
}
