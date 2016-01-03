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
    public class PrjCommandException : Exception
    {
        public PrjCommandException(string message)
            : base(message)
        {
        }
    }

    public class PrjCommandHandler
    {
        readonly PmView _view;

        public PrjCommandHandler(PmView view)
        {
            _view = view;
        }

        public IEnumerator<T> ProcessPrjCommandForResult<T>(string statusName, IEnumerator prjTask)
        {
            return CoRoutine.Wrap<T>(ProcessPrjCommand(statusName, prjTask));
        }

        public IEnumerator ProcessPrjCommand(string statusName, IEnumerator prjTask)
        {
            Assert.IsNull(_view.BlockedStatusMessage);
            _view.BlockedStatusMessage = statusName;

            while (prjTask.MoveNext())
            {
                if (prjTask.Current is PrjHelperResponse)
                {
                    Assert.That(!prjTask.MoveNext());
                    break;
                }

                if (prjTask.Current != null)
                {
                    Assert.IsType<List<string>>(prjTask.Current);
                    var outputLines = (List<string>)prjTask.Current;

                    if (outputLines.Count > 0)
                    {
                        _view.BlockedStatusMessage = outputLines.Last();
                    }
                }

                yield return null;
            }

            Assert.IsType<PrjHelperResponse>(prjTask.Current);
            var response = (PrjHelperResponse)prjTask.Current;

            // Refresh assets regardless of what kind of PrjCommand this was
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
                throw new PrjCommandException(response.ErrorMessage);
            }
        }
    }
}
