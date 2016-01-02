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
    public class UpmCommandException : Exception
    {
        public UpmCommandException(string message)
            : base(message)
        {
        }
    }

    public class UpmCommandHandler
    {
        readonly PmView _view;

        public UpmCommandHandler(PmView view)
        {
            _view = view;
        }

        public IEnumerator<T> ProcessUpmCommandForResult<T>(string statusName, IEnumerator upmTask)
        {
            return CoRoutine.Wrap<T>(ProcessUpmCommand(statusName, upmTask));
        }

        public IEnumerator ProcessUpmCommand(string statusName, IEnumerator upmTask)
        {
            Assert.IsNull(_view.BlockedStatusMessage);
            _view.BlockedStatusMessage = statusName;

            while (upmTask.MoveNext())
            {
                if (upmTask.Current is UpmHelperResponse)
                {
                    Assert.That(!upmTask.MoveNext());
                    break;
                }

                if (upmTask.Current != null)
                {
                    Assert.IsType<List<string>>(upmTask.Current);
                    var outputLines = (List<string>)upmTask.Current;

                    if (outputLines.Count > 0)
                    {
                        _view.BlockedStatusMessage = outputLines.Last();
                    }
                }

                yield return null;
            }

            Assert.IsType<UpmHelperResponse>(upmTask.Current);
            var response = (UpmHelperResponse)upmTask.Current;

            // Refresh assets regardless of what kind of UpmCommand this was
            // This is good because many commands can affect the project
            // Including installing a package, deleting a package, etc.
            AssetDatabase.Refresh();

            _view.BlockedStatusMessage = null;

            if (response.Succeeded)
            {
                yield return response.Result;
            }
            else
            {
                throw new UpmCommandException(response.ErrorMessage);
            }
        }
    }
}
