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
    public static class UpmInterface
    {
        public static bool UpdateLinks()
        {
            var result = RunUpm(CreateUpmRequest("updateLinks"));

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

            var result = RunUpm(CreateUpmRequestForPlatform("updateLinks", desiredPlatform));

            if (result.Succeeded)
            {
                result = RunUpm(CreateUpmRequestForPlatform("openUnity", desiredPlatform));
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

        public static UpmRequest CreateUpmRequest(string requestId)
        {
            return CreateUpmRequestForProjectAndPlatform(
                requestId,
                ProjenyEditorUtil.GetCurrentProjectName(),
                ProjenyEditorUtil.GetPlatformFromDirectoryName());
        }

        public static UpmRequest CreateUpmRequestForPlatform(
            string requestId, BuildTarget platform)
        {
            return CreateUpmRequestForProjectAndPlatform(
                requestId,
                ProjenyEditorUtil.GetCurrentProjectName(),
                platform);
        }

        public static UpmRequest CreateUpmRequestForProjectAndPlatform(
            string requestId, string projectName, BuildTarget platform)
        {
            return new UpmRequest()
            {
                RequestId = requestId,
                ProjectName = projectName,
                Platform = platform,
                ConfigPath = FindUpmConfigPath()
            };
        }

        public static void DisplayUpmError(string operationDescription, string errors)
        {
            var errorMessage = "UPM encountered errors when running '{0}'. Details: \n\n{1}".Fmt(operationDescription, errors);
            Log.Error("Projeny: {0}", errorMessage);
            EditorUtility.DisplayDialog("Error", errorMessage, "Ok");
        }

        static Process RunUpmCommonStart(UpmRequest request, out StringBuilder allOutputOutValue)
        {
            var startInfo = new ProcessStartInfo();

            startInfo.FileName = FindUpmExePath();

            var argStr = "\"{0}\" \"{1}\" {2} {3}"
                .Fmt(
                    request.ConfigPath, request.ProjectName,
                    ToPlatformDirStr(request.Platform), request.RequestId);

            if (request.Param1 != null)
            {
                argStr += " \"{0}\"".Fmt(request.Param1);
            }

            if (request.Param2 != null)
            {
                argStr += " \"{0}\"".Fmt(request.Param2);
            }

            startInfo.Arguments = argStr;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            UnityEngine.Debug.Log("Running command '{0} {1}'".Fmt(startInfo.FileName, startInfo.Arguments));

            Process proc = new Process();
            proc.StartInfo = startInfo;

            var allOutput = new StringBuilder();
            proc.OutputDataReceived += (sender, outputArgs) => allOutput.AppendLine(outputArgs.Data);
            proc.ErrorDataReceived += (sender, outputArgs) => allOutput.AppendLine(outputArgs.Data);

            allOutputOutValue = allOutput;
            return proc;
        }

        public static UpmResponse RunUpm(UpmRequest request)
        {
            StringBuilder allOutput;
            var proc = RunUpmCommonStart(request, out allOutput);

            proc.Start();

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            proc.WaitForExit();

            return RunUpmCommonEnd(proc, allOutput);
        }

        public static IEnumerator<UpmResponse> RunUpmAsync(UpmRequest request)
        {
            StringBuilder allOutput;
            var proc = RunUpmCommonStart(request, out allOutput);

            proc.EnableRaisingEvents = true;

            bool hasExited = false;
            proc.Exited += delegate
            {
                hasExited = true;
            };

            proc.Start();

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            while (!hasExited)
            {
                yield return null;
            }

            yield return RunUpmCommonEnd(proc, allOutput);
        }

        static UpmResponse RunUpmCommonEnd(Process proc, StringBuilder allOutput)
        {
            var finalOutput = allOutput.ToString().Trim();

            if (proc.ExitCode != 0)
            {
                return new UpmResponse()
                {
                    Succeeded = false,
                    ErrorMessage = finalOutput,
                };
            }

            return new UpmResponse()
            {
                Succeeded = true,
                Output = finalOutput,
            };
        }

        static string FindUpmExePath()
        {
            try
            {
                return PathUtil.FindExePath("UpmEditorApi.bat");
            }
            catch (FileNotFoundException)
            {
                throw new UpmException(
                    "Could not locate path to UPM.  Have you added 'projeny/Bin/Upm' to your environment PATH?  See documentation for details.");
            }
        }

        public static string FindUpmConfigPath()
        {
            foreach (var dirInfo in PathUtil.GetAllParentDirectories(Application.dataPath))
            {
                var configPath = Path.Combine(dirInfo.FullName, ProjenyEditorUtil.ConfigFileName);

                if (File.Exists(configPath))
                {
                    return configPath;
                }
            }

            throw new UpmException(
                "Could not locate {0} when searching from {1} upwards".Fmt(ProjenyEditorUtil.ConfigFileName, Application.dataPath));
        }

        static string ToPlatformDirStr(BuildTarget platform)
        {
            switch (platform)
            {
                case BuildTarget.StandaloneWindows:
                {
                    return "windows";
                }
                case BuildTarget.Android:
                {
                    return "android";
                }
                case BuildTarget.WebPlayer:
                {
                    return "webplayer";
                }
                case BuildTarget.WebGL:
                {
                    return "webgl";
                }
                case BuildTarget.StandaloneOSXUniversal:
                {
                    return "osx";
                }
                case BuildTarget.iOS:
                {
                    return "ios";
                }
                case BuildTarget.StandaloneLinux:
                {
                    return "linux";
                }
            }

            throw new NotImplementedException();
        }

        // Async Methods
        public static IEnumerator<Boolean> UpdateLinksAsync()
        {
            return CoRoutine.Wrap<Boolean>(UpdateLinksAsyncInternal());
        }

        static IEnumerator UpdateLinksAsyncInternal()
        {
            var req = CreateUpmRequest("updateLinks");

            var result = RunUpmAsync(req);
            yield return result;

            if (result.Current.Succeeded)
            {
                AssetDatabase.Refresh();
                Log.Info("Projeny: Updated directory links");
                yield return true;
            }
            else
            {
                DisplayUpmError("Updating Directory Links", result.Current.ErrorMessage);
                yield return false;
            }
        }

        public static IEnumerator<Boolean> InstallReleaseAsync(string name, string version)
        {
            return CoRoutine.Wrap<Boolean>(InstallReleaseAsyncInternal(name, version));
        }

        static IEnumerator InstallReleaseAsyncInternal(string name, string version)
        {
            var req = CreateUpmRequest("installRelease");

            req.Param1 = name;
            req.Param2 = version;

            var result = RunUpmAsync(req);
            yield return result;

            if (result.Current.Succeeded)
            {
                Log.Info("Installed new release '{0}' ({1})".Fmt(name, version));
                yield return true;
            }
            else
            {
                DisplayUpmError("Installing Release '{0}' ({1})".Fmt(name, version), result.Current.ErrorMessage);
                yield return false;
            }
        }

        // NOTE: Returns null on failure
        public static IEnumerator<List<ReleaseInfo>> LookupReleaseListAsync()
        {
            return CoRoutine.Wrap<List<ReleaseInfo>>(LookupReleaseListAsyncInternal());
        }

        static IEnumerator LookupReleaseListAsyncInternal()
        {
            var req = CreateUpmRequest("listReleases");

            var result = RunUpmAsync(req);
            yield return result;

            if (result.Current.Succeeded)
            {
                var docs = result.Current.Output
                    .Split(new string[] { "---" }, StringSplitOptions.None);

                yield return docs
                    .Select(x => YamlSerializer.Deserialize<ReleaseInfo>(x))
                    .Where(x => x != null).ToList();
            }
            else
            {
                DisplayUpmError("Looking up all releases", result.Current.ErrorMessage);
                yield return null;
            }
        }

        // NOTE: Returns null on failure
        public static IEnumerator<List<PackageInfo>> LookupPackagesListAsync()
        {
            return CoRoutine.Wrap<List<PackageInfo>>(LookupPackagesListAsyncInternal());
        }

        static IEnumerator LookupPackagesListAsyncInternal()
        {
            var result = RunUpmAsync(CreateUpmRequest("listPackages"));
            yield return result;

            if (!result.Current.Succeeded)
            {
                DisplayUpmError("Package lookup", result.Current.ErrorMessage);
                yield return null;
                yield break;
            }

            var docs = result.Current.Output
                .Split(new string[] { "---" }, StringSplitOptions.None);

            yield return docs
                .Select(x => YamlSerializer.Deserialize<PackageInfo>(x)).Where(x => x != null).ToList();
        }

        public class UpmException : Exception
        {
            public UpmException(string errorMessage)
                : base(errorMessage)
            {
            }
        }

        public class UpmResponse
        {
            public bool Succeeded;
            public string ErrorMessage;
            public string Output;
        }

        public class UpmRequest
        {
            public string RequestId;
            public string ProjectName;
            public BuildTarget Platform;
            public string ConfigPath;
            public string Param1;
            public string Param2;
        }
    }
}
