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
    public class PmReleasesHandler
    {
        readonly PrjCommandHandler _prjCommandHandler;
        readonly PmModel _model;

        public PmReleasesHandler(
            PmModel model,
            PrjCommandHandler prjCommandHandler)
        {
            _prjCommandHandler = prjCommandHandler;
            _model = model;
        }

        public IEnumerator RefreshReleasesAsync()
        {
            var response = _prjCommandHandler.ProcessPrjCommandForResult<List<ReleaseInfo>>(
                "Looking up release list", PrjHelper.LookupReleaseListAsync());
            yield return response;

            _model.SetReleases(response.Current);
        }
    }
}



