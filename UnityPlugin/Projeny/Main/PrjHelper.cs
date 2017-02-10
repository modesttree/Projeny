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

            // This sometimes causes out of memory issues for reasons unknown so just let user
            // manually refresh
            //AssetDatabase.Refresh();

            if (!result.Succeeded)
            {
                DisplayPrjError("Updating directory links", result.ErrorMessage);
                return false;
            }

            return true;
        }

        public static void ChangeProject(string projectName)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // They hit cancel in the save dialog
                return;
            }

            var result = PrjInterface.RunPrj(PrjInterface.CreatePrjRequestForProject("updateLinks", projectName));

            if (result.Succeeded)
            {
                result = PrjInterface.RunPrj(PrjInterface.CreatePrjRequestForProject("openUnity", projectName));

                if (result.Succeeded)
                {
                    EditorApplication.Exit(0);
                }
            }

            if (!result.Succeeded)
            {
                DisplayPrjError(
                    "Changing project to '{0}'"
                    .Fmt(projectName), result.ErrorMessage);
            }
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

        public static IEnumerator OpenUnityForProjectAsync(string projectName)
        {
            var runner = PrjInterface.RunPrjAsync(
                PrjInterface.CreatePrjRequestForProject("openUnity", projectName));

            while (runner.MoveNext() && !(runner.Current is PrjResponse))
            {
                yield return runner.Current;
            }

            yield return CreateStandardResponse((PrjResponse)runner.Current);
        }

        // NOTE: It's up to the caller to call AssetDatabase.Refresh()
        public static IEnumerator UpdateLinksAsync()
        {
            return UpdateLinksAsyncInternal(
                PrjInterface.CreatePrjRequest("updateLinks"));
        }

        public static IEnumerator UpdateLinksAsyncForProject(string projectName)
        {
            return UpdateLinksAsyncInternal(
                PrjInterface.CreatePrjRequestForProject("updateLinks", projectName));
        }

        static IEnumerator UpdateLinksAsyncInternal(PrjRequest req)
        {
            var runner = PrjInterface.RunPrjAsync(req);

            while (runner.MoveNext() && !(runner.Current is PrjResponse))
            {
                yield return runner.Current;
            }

            yield return CreateStandardResponse((PrjResponse)runner.Current);
        }

        // NOTE: It's up to the calling code to update the solution first
        public static IEnumerator OpenCustomSolutionAsync()
        {
            var runner = PrjInterface.RunPrjAsync(PrjInterface.CreatePrjRequest("openCustomSolution"));

            while (runner.MoveNext() && !(runner.Current is PrjResponse))
            {
                yield return runner.Current;
            }

            yield return CreateStandardResponse((PrjResponse)runner.Current);
        }

        public static IEnumerator UpdateCustomSolutionAsync()
        {
            // Need the unity solution for defines and references
            ProjenyEditorUtil.ForceGenerateUnitySolution();

            var runner = PrjInterface.RunPrjAsync(PrjInterface.CreatePrjRequest("updateCustomSolution"));

            while (runner.MoveNext() && !(runner.Current is PrjResponse))
            {
                yield return runner.Current;
            }

            yield return CreateStandardResponse((PrjResponse)runner.Current);
        }

        public static IEnumerator InstallReleaseAsync(string packageRoot, ReleaseInfo info)
        {
            var req = PrjInterface.CreatePrjRequest("installRelease");
            req.Param1 = info.Id;
            req.Param2 = packageRoot;

            if (info.HasVersionCode)
            {
                req.Param3 = info.VersionCode.ToString();
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

        public static IEnumerator CreateProjectAsync(string projectName, bool duplicateSettings)
        {
            var request = PrjInterface.CreatePrjRequest("createProject");

            request.Param1 = projectName;
            request.Param2 = duplicateSettings.ToString();

            var runner = PrjInterface.RunPrjAsync(request);

            while (runner.MoveNext() && !(runner.Current is PrjResponse))
            {
                yield return runner.Current;
            }

            yield return CreateStandardResponse((PrjResponse)runner.Current);
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
                        .Select(x => PrjSerializer.DeserializePackageFolderInfo(x))
                        .Where(x => x != null).ToList());
            }
            else
            {
                yield return PrjHelperResponse.Error(response.ErrorMessage);
            }
        }
    }
}
