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
    public class PmController : IDisposable
    {
        readonly PmModel _model;
        readonly PmView.Model _viewModel;

        bool _isFirstLoad;

        PmView _view;

        PmModelViewSyncer _viewModelSyncer;

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
        PmDragDropHandler _dragDropHandler;
        PmInputHandler _inputHandler;

        public PmController(PmModel model, PmView.Model viewModel, bool isFirstLoad)
        {
            _model = model;
            _viewModel = viewModel;
            _isFirstLoad = isFirstLoad;
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
            _dragDropHandler.Initialize();

            if (_isFirstLoad)
            {
                //_asyncProcessor.Process(RefreshAll(), "Refreshing Packages");
            }
        }

        IEnumerator RefreshAll()
        {
            _projectHandler.RefreshProject();
            yield return _packageHandler.RefreshPackagesAsync();
            yield return _releasesHandler.RefreshReleasesAsync();
        }

        void SetupDependencies()
        {
            // We could use a DI framework like zenject here but it's overkill
            // and also we'd like to keep the dependencies for Projeny low
            // So just do poor man's DI instead
            _asyncProcessor = new AsyncProcessor();

            _view = new PmView(_viewModel);

            _upmCommandHandler = new UpmCommandHandler(_view);

            _packageHandler = new PmPackageHandler(_model, _upmCommandHandler, _view);
            _releasesHandler = new PmReleasesHandler(_model, _upmCommandHandler);

            _inputHandler = new PmInputHandler(_view, _model, _packageHandler, _asyncProcessor);
            _viewErrorHandler = new PmViewErrorHandler(_view, _asyncProcessor);
            _viewAsyncHandler = new PmViewAsyncHandler(_view, _asyncProcessor);
            _viewModelSyncer = new PmModelViewSyncer(_model, _view);
            _projectHandler = new PmProjectHandler(_model, _view);
            _dragDropHandler = new PmDragDropHandler(_model, _view, _asyncProcessor, _packageHandler, _upmCommandHandler);
            _packageViewHandler = new PmPackageViewHandler(_view, _asyncProcessor, _packageHandler, _upmCommandHandler);

            _projectViewHandler = new PmProjectViewHandler(
                _model, _view, _projectHandler, _asyncProcessor,
                _upmCommandHandler, _viewErrorHandler);

            _releasesViewHandler = new PmReleasesViewHandler(
                _model, _view, _asyncProcessor, _releasesHandler, _packageHandler, _upmCommandHandler);
        }

        public void Dispose()
        {
            _viewModelSyncer.Dispose();
            _releasesViewHandler.Dispose();
            _dragDropHandler.Dispose();
        }

        public void Update()
        {
            _asyncProcessor.Tick();

            _projectViewHandler.Update();
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

