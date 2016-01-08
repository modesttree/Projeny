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
        readonly PmViewErrorHandler _errorHandler;
        readonly PrjCommandHandler _prjCommandHandler;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmModel _model;
        readonly PmView _view;

        readonly EventManager _eventManager = new EventManager();

        public PmVsSolutionViewHandler(
            PmModel model,
            PmView view,
            AsyncProcessor asyncProcessor,
            PrjCommandHandler prjCommandHandler,
            PmViewErrorHandler errorHandler,
            PmProjectHandler projectHandler)
        {
            _projectHandler = projectHandler;
            _errorHandler = errorHandler;
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

            yield return new ContextMenuItem(
                !selected.IsEmpty(), "Remove", false, OnContextMenuDeleteSelected);
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
                OpenCustomSolutionAsync(), "Opening Visual Studio Solution");
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
                UpdateCustomSolutionAsync(), "Updating Visual Studio Solution");
        }

        IEnumerator UpdateCustomSolutionAsync()
        {
            _projectHandler.OverwriteConfig();

            return _prjCommandHandler.ProcessPrjCommand(
                "Updating solution", PrjHelper.UpdateCustomSolutionAsync());
        }
    }
}
