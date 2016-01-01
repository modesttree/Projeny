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
    public class UpmHelperResponse
    {
        public readonly bool Succeeded;
        public readonly string ErrorMessage;
        // The type here varies by request type
        public readonly object Result;

        UpmHelperResponse(
            bool succeeded, string errorMessage, object result)
        {
            Succeeded = succeeded;
            ErrorMessage = errorMessage;
            Result = result;
        }

        public static UpmHelperResponse Error(string errorMessage)
        {
            return new UpmHelperResponse(false, errorMessage, null);
        }

        public static UpmHelperResponse Success(object result = null)
        {
            return new UpmHelperResponse(true, null, result);
        }
    }

    public static class UpmHelper
    {
        public static bool UpdateLinks()
        {
            var result = UpmInterface.RunUpm(UpmInterface.CreateUpmRequest("updateLinks"));

            AssetDatabase.Refresh();

            if (!result.Succeeded)
            {
                DisplayUpmError("Updating directory links", result.ErrorMessage);
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

            var result = UpmInterface.RunUpm(UpmInterface.CreateUpmRequestForPlatform("updateLinks", desiredPlatform));

            if (result.Succeeded)
            {
                result = UpmInterface.RunUpm(UpmInterface.CreateUpmRequestForPlatform("openUnity", desiredPlatform));
            }

            if (result.Succeeded)
            {
                EditorApplication.Exit(0);
            }
            else
            {
                DisplayUpmError(
                    "Changing platform to '{0}'"
                    .Fmt(desiredPlatform.ToString()), result.ErrorMessage);
            }
        }

        public static void DisplayUpmError(string operationDescription, string errors)
        {
            var errorMessage = "Operation aborted.  UPM encountered errors when running '{0}'. Details: \n\n{1}".Fmt(operationDescription, errors);
            Log.Error("Projeny: {0}", errorMessage);
            EditorUtility.DisplayDialog("Error", errorMessage, "Ok");
        }

        // NOTE: It's up to the caller to call AssetDatabase.Refresh()
        public static IEnumerator UpdateLinksAsync()
        {
            var req = UpmInterface.CreateUpmRequest("updateLinks");

            var runner = UpmInterface.RunUpmAsync(req);

            while (runner.MoveNext() && !(runner.Current is UpmResponse))
            {
                yield return runner.Current;
            }

            yield return CreateStandardResponse((UpmResponse)runner.Current);
        }

        public static IEnumerator InstallReleaseAsync(ReleaseInfo info)
        {
            var req = UpmInterface.CreateUpmRequest("installRelease");
            req.Param1 = info.Id;

            if (info.HasVersionCode)
            {
                req.Param2 = info.VersionCode.ToString();
            }

            var runner = UpmInterface.RunUpmAsync(req);

            while (runner.MoveNext() && !(runner.Current is UpmResponse))
            {
                yield return runner.Current;
            }

            yield return CreateStandardResponse((UpmResponse)runner.Current);
        }

        static UpmHelperResponse CreateStandardResponse(UpmResponse response)
        {
            if (response.Succeeded)
            {
                return UpmHelperResponse.Success();
            }

            return UpmHelperResponse.Error(response.ErrorMessage);
        }

        public static IEnumerator CreatePackageAsync(string name)
        {
            var req = UpmInterface.CreateUpmRequest("createPackage");

            req.Param1 = name;

            var runner = UpmInterface.RunUpmAsync(req);

            while (runner.MoveNext() && !(runner.Current is UpmResponse))
            {
                yield return runner.Current;
            }

            yield return CreateStandardResponse((UpmResponse)runner.Current);
        }

        public static IEnumerator DeletePackagesAsync(List<PackageInfo> infos)
        {
            foreach (var info in infos)
            {
                Log.Debug("Deleting package '{0}'".Fmt(info.Name));

                var req = UpmInterface.CreateUpmRequest("deletePackage");

                req.Param1 = info.Name;

                var runner = UpmInterface.RunUpmAsync(req);

                while (runner.MoveNext() && !(runner.Current is UpmResponse))
                {
                    yield return runner.Current;
                }

                var response = (UpmResponse)runner.Current;

                if (response.Succeeded)
                {
                    Log.Info("Deleted package '{0}'".Fmt(info.Name));
                }
                else
                {
                    yield return UpmHelperResponse.Error(response.ErrorMessage);
                    yield break;
                }
            }

            yield return UpmHelperResponse.Success();
        }

        // Yields strings indicating status
        // With final yield of type UpmHelperResponse with value type List<ReleaseInfo>
        public static IEnumerator LookupReleaseListAsync()
        {
            var runner = UpmInterface.RunUpmAsync(UpmInterface.CreateUpmRequest("listReleases"));

            while (runner.MoveNext() && !(runner.Current is UpmResponse))
            {
                yield return runner.Current;
            }

            var response = (UpmResponse)runner.Current;

            if (response.Succeeded)
            {
                var docs = response.Output
                    .Split(new string[] { "---" }, StringSplitOptions.None);

                yield return UpmHelperResponse.Success(
                    docs
                        .Select(x => UpmSerializer.DeserializeReleaseInfo(x))
                        .Where(x => x != null).ToList());
            }
            else
            {
                yield return UpmHelperResponse.Error(response.ErrorMessage);
            }
        }

        // NOTE: Returns null on failure
        public static IEnumerator LookupPackagesListAsync()
        {
            var runner = UpmInterface.RunUpmAsync(UpmInterface.CreateUpmRequest("listPackages"));

            while (runner.MoveNext() && !(runner.Current is UpmResponse))
            {
                yield return runner.Current;
            }

            var response = (UpmResponse)runner.Current;

            if (response.Succeeded)
            {
                var docs = response.Output
                    .Split(new string[] { "---" }, StringSplitOptions.None);

                yield return UpmHelperResponse.Success(
                    docs
                        .Select(x => UpmSerializer.DeserializePackageInfo(x))
                        .Where(x => x != null).ToList());
            }
            else
            {
                yield return UpmHelperResponse.Error(response.ErrorMessage);
            }
        }
    }
}
