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
        readonly UpmCommandHandler _upmCommandHandler;
        readonly PmModel _model;

        public PmReleasesHandler(
            PmModel model,
            UpmCommandHandler upmCommandHandler)
        {
            _upmCommandHandler = upmCommandHandler;
            _model = model;
        }

        public IEnumerator RefreshReleasesAsync()
        {
            var response = _upmCommandHandler.ProcessUpmCommandForResult<List<ReleaseInfo>>(
                "Looking up release list", UpmHelper.LookupReleaseListAsync());
            yield return response;

            _model.SetReleases(response.Current);
        }
    }
}



