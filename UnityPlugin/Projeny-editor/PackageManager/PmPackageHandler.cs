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
        readonly PmView _view;
        readonly UpmCommandHandler _upmCommandHandler;
        readonly PmModel _model;

        public PmPackageHandler(
            PmModel model,
            UpmCommandHandler upmCommandHandler,
            PmView view)
        {
            _view = view;
            _upmCommandHandler = upmCommandHandler;
            _model = model;
        }

        public IEnumerator DeletePackages(List<PackageInfo> packages)
        {
            var choice = _view.PromptForUserChoice(
                "<color=yellow>Are you sure you want to delete the following packages?</color>\n\n{0}\n\n<color=yellow>Please note the following:</color>\n\n- This change is not undoable\n- Any changes that you've made since installing will be lost\n- Any projects or other packages that still depend on this package may be put in an invalid state by deleting it".Fmt(packages.Select(x => "- " + x.Name).Join("\n")),
                new[] { "Delete", "Cancel" }, null, "DeleteSelectedPopupTextStyle", 0, 1);

            yield return choice;

            if (choice.Current == 0)
            {
                yield return _upmCommandHandler.ProcessUpmCommand(
                    "Deleting packages", UpmHelper.DeletePackagesAsync(packages));
                yield return RefreshPackagesAsync();
            }
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



