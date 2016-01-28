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
    public class PmCompositionRoot : IDisposable
    {
        readonly PmModel _model;
        readonly PmView.Model _viewModel;

        bool _isFirstLoad;

        PmView _view;

        PmModelViewSyncer _viewModelSyncer;

        AsyncProcessor _asyncProcessor;

        PmProjectViewHandler _projectViewHandler;
        PmVsSolutionViewHandler _solutionViewHandler;
        PmProjectHandler _projectHandler;

        PmReleasesHandler _releasesHandler;
        PmReleasesViewHandler _releasesViewHandler;

        PrjCommandHandler _prjCommandHandler;
        PmViewAsyncHandler _viewAsyncHandler;
        PmViewErrorHandler _viewErrorHandler;
        PmPackageViewHandler _packageViewHandler;
        PmPackageHandler _packageHandler;
        PmDragDropHandler _dragDropHandler;
        PmInputHandler _inputHandler;
        PmSettings _settings;
        PmCreateNewProjectPopupHandler _createNewProjectHandler;
        PmWindowInitializer _windowInitializer;

        public PmCompositionRoot(PmModel model, PmView.Model viewModel, bool isFirstLoad)
        {
            _model = model;
            _viewModel = viewModel;
            _isFirstLoad = isFirstLoad;
        }

        public void ShowCreateNewProjectPopup()
        {
            _createNewProjectHandler.ShowCreateNewProjectPopup();
        }

        public void Initialize()
        {
            SetupDependencies();
            Start();
        }

        public void Dispose()
        {
            _viewModelSyncer.Dispose();
            _releasesViewHandler.Dispose();
            _dragDropHandler.Dispose();
            _viewErrorHandler.Dispose();
        }

        void Start()
        {
            _viewModelSyncer.Initialize();
            _projectViewHandler.Initialize();
            _solutionViewHandler.Initialize();
            _packageViewHandler.Initialize();
            _releasesViewHandler.Initialize();
            _dragDropHandler.Initialize();
            _viewErrorHandler.Initialize();

            if (_isFirstLoad)
            {
                _windowInitializer.Initialize();
            }
        }

        void SetupDependencies()
        {
            // We could use a DI framework like zenject here but it's overkill
            // and also we'd like to keep the dependencies for Projeny low
            // So just do poor man's DI instead
            _asyncProcessor = new AsyncProcessor();

            _settings = Resources.Load<PmSettings>("Projeny/PmSettings");
            _view = new PmView(_viewModel, _settings);

            _prjCommandHandler = new PrjCommandHandler(_view);

            _packageHandler = new PmPackageHandler(_model, _prjCommandHandler, _view);
            _releasesHandler = new PmReleasesHandler(_model, _prjCommandHandler);

            _inputHandler = new PmInputHandler(_view, _model, _packageHandler, _asyncProcessor);
            _viewErrorHandler = new PmViewErrorHandler(_view, _asyncProcessor);
            _viewAsyncHandler = new PmViewAsyncHandler(_view, _asyncProcessor, _settings);
            _viewModelSyncer = new PmModelViewSyncer(_model, _view, _settings);
            _projectHandler = new PmProjectHandler(_model, _view);
            _dragDropHandler = new PmDragDropHandler(_model, _view, _asyncProcessor, _packageHandler, _prjCommandHandler);
            _packageViewHandler = new PmPackageViewHandler(_view, _asyncProcessor, _packageHandler, _prjCommandHandler, _settings);

            _windowInitializer = new PmWindowInitializer(_projectHandler, _packageHandler, _releasesHandler, _asyncProcessor);
            _createNewProjectHandler = new PmCreateNewProjectPopupHandler(_view, _asyncProcessor, _prjCommandHandler, _windowInitializer);

            _projectViewHandler = new PmProjectViewHandler(
                _model, _view, _projectHandler, _asyncProcessor,
                _prjCommandHandler, _viewErrorHandler);

            _solutionViewHandler = new PmVsSolutionViewHandler(
                _model, _view, _asyncProcessor,
                _prjCommandHandler, _viewErrorHandler, _projectHandler);

            _releasesViewHandler = new PmReleasesViewHandler(
                _model, _view, _asyncProcessor, _releasesHandler, _packageHandler, _prjCommandHandler, _settings);
        }

        public void Update()
        {
            _asyncProcessor.Tick();

            _projectViewHandler.Update();
            _solutionViewHandler.Update();
            _packageViewHandler.Update();

            _viewAsyncHandler.Update();
            _releasesViewHandler.Update();
            _dragDropHandler.Update();

            // Do this last so all model updates get forwarded to view
            _viewModelSyncer.Update();

            _view.Update();
        }

        public void OnGUI(Rect fullRect)
        {
            _view.OnGUI(fullRect);

            _inputHandler.CheckForKeypresses();
        }
    }
}
