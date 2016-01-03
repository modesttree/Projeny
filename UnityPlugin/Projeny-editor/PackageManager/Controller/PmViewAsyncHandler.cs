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
    public class PmViewAsyncHandler
    {
        readonly PmSettings _pmSettings;
        readonly AsyncProcessor _asyncProcessor;
        readonly PmView _view;

        bool _isBlocked;
        float _isBlockedStartTime;

        public PmViewAsyncHandler(
            PmView view,
            AsyncProcessor asyncProcessor,
            PmSettings pmSettings)
        {
            _pmSettings = pmSettings;
            _asyncProcessor = asyncProcessor;
            _view = view;
        }

        bool IsBlocked
        {
            get
            {
                return _isBlocked;
            }
            set
            {
                if (_isBlocked != value)
                {
                    _isBlockedStartTime = Time.realtimeSinceStartup;
                    _isBlocked = value;
                }
            }
        }

        public void Update()
        {
            IsBlocked = _asyncProcessor.IsBlocking;
            _view.IsBlocked = IsBlocked;

            if (IsBlocked)
            {
                _view.ShowBlockedPopup = ShouldShowBlockedPopup();
                _view.BlockedStatusTitle = _asyncProcessor.StatusTitle;
            }
        }

        bool ShouldShowBlockedPopup()
        {
            // We only wnat to display the popup if enough time has passed to avoid short flashes for quick async tasks
            return Time.realtimeSinceStartup - _isBlockedStartTime > _pmSettings.View.ProcessingPopupDelayTime;
        }
    }
}
