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
    public class PmViewErrorHandler
    {
        readonly PmView _view;
        readonly AsyncProcessor _asyncProcessor;
        bool _isDisplayingError;

        public PmViewErrorHandler(
            PmView view,
            AsyncProcessor asyncProcessor)
        {
            _view = view;
            _asyncProcessor = asyncProcessor;
        }

        public void Initialize()
        {
            _asyncProcessor.OnException += OnAsyncException;
        }

        public void Dispose()
        {
            _asyncProcessor.OnException -= OnAsyncException;
        }

        void OnAsyncException(Exception exception)
        {
            Log.ErrorException(exception);

            if (exception is CoRoutineException)
            {
                // Don't bother showing the coroutine trace - it is in the log if you want it
                var coroutineException = (CoRoutineException)exception;
                exception = coroutineException.InnerException;
            }

            DisplayErrorInternal(exception.GetFullMessage());
        }

        public void DisplayError(string message)
        {
            Log.Error("Projeny: " + message);
            DisplayErrorInternal(message);
        }

        void DisplayErrorInternal(string message)
        {
            // Do not display errors on top of each other
            // In those cases it will still be in the log and that's enough
            if (!_isDisplayingError)
            {
                _asyncProcessor.Process(
                    DisplayErrorInternalAsync(message));
            }
        }

        IEnumerator DisplayErrorInternalAsync(string message)
        {
            Assert.That(!_isDisplayingError);
            _isDisplayingError = true;

            yield return _view.AlertUser(message, "<color=red>Error!</color>");

            _isDisplayingError = false;
        }
    }
}
