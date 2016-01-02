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
    public class PmReleasesViewHandler : IDisposable
    {
        readonly PmReleasesHandler _releasesHandler;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmView _view;
        readonly PmModel _model;
        readonly EventManager _eventManager = new EventManager();

        public PmReleasesViewHandler(
            PmModel model,
            PmView view,
            AsyncProcessor asyncProcessor,
            PmReleasesHandler releasesHandler)
        {
            _releasesHandler = releasesHandler;
            _asyncProcessor = asyncProcessor;
            _view = view;
            _model = model;
        }

        public void Initialize()
        {
            _view.ClickedRefreshReleaseList += _eventManager.Add(OnClickedRefreshReleaseList, EventQueueMode.LatestOnly);
        }

        public void Dispose()
        {
            _view.ClickedRefreshReleaseList -= _eventManager.Remove(OnClickedRefreshReleaseList);
            _eventManager.AssertIsEmpty();
        }

        public void Update()
        {
            _eventManager.Flush();
        }

        public void OnClickedRefreshReleaseList()
        {
            _asyncProcessor.Process(_releasesHandler.RefreshReleasesAsync(), "Refreshing Release List");
        }
    }
}

