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
    public class PmPackageHandler
    {
        readonly UpmCommandHandler _upmCommandHandler;
        readonly PmModel _model;

        public PmPackageHandler(
            PmModel model,
            UpmCommandHandler upmCommandHandler)
        {
            _upmCommandHandler = upmCommandHandler;
            _model = model;
        }

        public IEnumerator DeletePackages(List<PackageInfo> packages)
        {
            yield return _upmCommandHandler.ProcessUpmCommand(
                "Deleting packages", UpmHelper.DeletePackagesAsync(packages));
            yield return RefreshPackagesAsync();
        }

        public IEnumerator RefreshPackagesAsync()
        {
            var allPackages = _upmCommandHandler.ProcessUpmCommandForResult<List<PackageInfo>>(
                "Looking up package list", UpmHelper.LookupPackagesListAsync());
            yield return allPackages;

            _model.SetPackages(allPackages.Current);
        }
    }
}



