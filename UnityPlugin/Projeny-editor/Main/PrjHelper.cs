using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using Projeny.Internal;

namespace Projeny
{
    public class PrjHelperResponse
    {
        public readonly bool Succeeded;
        public readonly string ErrorMessage;
        // The type here varies by request type
        public readonly object Result;

        PrjHelperResponse(
            bool succeeded, string errorMessage, object result)
        {
            Succeeded = succeeded;
            ErrorMessage = errorMessage;
            Result = result;
        }

        public static PrjHelperResponse Error(string errorMessage)
        {
            return new PrjHelperResponse(false, errorMessage, null);
        }

        public static PrjHelperResponse Success(object result = null)
        {
            return new PrjHelperResponse(true, null, result);
        }
    }

    public static class PrjHelper
    {
        public static bool UpdateLinks()
        {
            var result = PrjInterface.RunPrj(PrjInterface.CreatePrjRequest("updateLinks"));

            AssetDatabase.Refresh();

            if (!result.Succeeded)
            {
                DisplayPrjError("Updating directory links", result.ErrorMessage);
                return false;
            }

            return true;
        }

        public static void ChangePlatform(BuildTarget desiredPlatform)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // They hit cancel in the save dialog
                return;
            }

            if (ProjenyEditorUtil.GetPlatformFromDirectoryName() == desiredPlatform)
            {
                UnityEngine.Debug.Log("Projeny: Already at the desired platform, no need to change project.");
                return;
            }

            var result = PrjInterface.RunPrj(PrjInterface.CreatePrjRequestForPlatform("updateLinks", desiredPlatform));

            if (result.Succeeded)
            {
                result = PrjInterface.RunPrj(PrjInterface.CreatePrjRequestForPlatform("openUnity", desiredPlatform));
            }

            if (result.Succeeded)
            {
                EditorApplication.Exit(0);
            }
            else
            {
                DisplayPrjError(
                    "Changing platform to '{0}'"
                    .Fmt(desiredPlatform.ToString()), result.ErrorMessage);
            }
        }

        public static void DisplayPrjError(string operationDescription, string errors)
        {
            var errorMessage = "Operation aborted.  Projeny encountered errors when running '{0}'. Details: \n\n{1}".Fmt(operationDescription, errors);
            Log.Error("Projeny: {0}", errorMessage);
            EditorUtility.DisplayDialog("Error", errorMessage, "Ok");
        }

        // NOTE: It's up to the caller to call AssetDatabase.Refresh()
        public static IEnumerator UpdateLinksAsync()
        {
            var req = PrjInterface.CreatePrjRequest("updateLinks");

            var runner = PrjInterface.RunPrjAsync(req);

            while (runner.MoveNext() && !(runner.Current is PrjResponse))
            {
                yield return runner.Current;
            }

            yield return CreateStandardResponse((PrjResponse)runner.Current);
        }

        public static IEnumerator InstallReleaseAsync(ReleaseInfo info)
        {
            var req = PrjInterface.CreatePrjRequest("installRelease");
            req.Param1 = info.Id;

            if (info.HasVersionCode)
            {
                req.Param2 = info.VersionCode.ToString();
            }

            var runner = PrjInterface.RunPrjAsync(req);

            while (runner.MoveNext() && !(runner.Current is PrjResponse))
            {
                yield return runner.Current;
            }

            yield return CreateStandardResponse((PrjResponse)runner.Current);
        }

        static PrjHelperResponse CreateStandardResponse(PrjResponse response)
        {
            if (response.Succeeded)
            {
                return PrjHelperResponse.Success();
            }

            return PrjHelperResponse.Error(response.ErrorMessage);
        }

        public static IEnumerator CreatePackageAsync(string name)
        {
            var req = PrjInterface.CreatePrjRequest("createPackage");

            req.Param1 = name;

            var runner = PrjInterface.RunPrjAsync(req);

            while (runner.MoveNext() && !(runner.Current is PrjResponse))
            {
                yield return runner.Current;
            }

            yield return CreateStandardResponse((PrjResponse)runner.Current);
        }

        public static IEnumerator DeletePackagesAsync(List<PackageInfo> infos)
        {
            foreach (var info in infos)
            {
                Log.Debug("Deleting package '{0}'".Fmt(info.Name));

                var req = PrjInterface.CreatePrjRequest("deletePackage");

                req.Param1 = info.Name;

                var runner = PrjInterface.RunPrjAsync(req);

                while (runner.MoveNext() && !(runner.Current is PrjResponse))
                {
                    yield return runner.Current;
                }

                var response = (PrjResponse)runner.Current;

                if (response.Succeeded)
                {
                    Log.Info("Deleted package '{0}'".Fmt(info.Name));
                }
                else
                {
                    yield return PrjHelperResponse.Error(response.ErrorMessage);
                    yield break;
                }
            }

            yield return PrjHelperResponse.Success();
        }

        // Yields strings indicating status
        // With final yield of type PrjHelperResponse with value type List<ReleaseInfo>
        public static IEnumerator LookupReleaseListAsync()
        {
            var runner = PrjInterface.RunPrjAsync(PrjInterface.CreatePrjRequest("listReleases"));

            while (runner.MoveNext() && !(runner.Current is PrjResponse))
            {
                yield return runner.Current;
            }

            var response = (PrjResponse)runner.Current;

            if (response.Succeeded)
            {
                var docs = response.Output
                    .Split(new string[] { "---" }, StringSplitOptions.None);

                yield return PrjHelperResponse.Success(
                    docs.Select(x => x.Trim())
                        .Where(x => x.Length > 0)
                        .Select(x => PrjSerializer.DeserializeReleaseInfo(x))
                        .Where(x => x != null).ToList());
            }
            else
            {
                yield return PrjHelperResponse.Error(response.ErrorMessage);
            }
        }

        // NOTE: Returns null on failure
        public static IEnumerator LookupPackagesListAsync()
        {
            var runner = PrjInterface.RunPrjAsync(PrjInterface.CreatePrjRequest("listPackages"));

            while (runner.MoveNext() && !(runner.Current is PrjResponse))
            {
                yield return runner.Current;
            }

            var response = (PrjResponse)runner.Current;

            if (response.Succeeded)
            {
                var docs = response.Output
                    .Split(new string[] { "---" }, StringSplitOptions.None);

                yield return PrjHelperResponse.Success(
                    docs
                        .Select(x => PrjSerializer.DeserializePackageInfo(x))
                        .Where(x => x != null).ToList());
            }
            else
            {
                yield return PrjHelperResponse.Error(response.ErrorMessage);
            }
        }
    }
}
