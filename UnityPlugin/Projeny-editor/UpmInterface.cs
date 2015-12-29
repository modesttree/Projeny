using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;
using Projeny.Internal;

namespace Projeny
{
    public static class UpmInterface
    {
        public const string ConfigFileName = "upm.yaml";

        public const string ProjectConfigFileName = "project.yaml";
        public const string ProjectConfigUserFileName = "projectUser.yaml";

        public static IEnumerator<Boolean> UpdateLinksAsync()
        {
            return CoRoutine.Wrap<Boolean>(UpdateLinksAsyncInternal());
        }

        static IEnumerator UpdateLinksAsyncInternal()
        {
            var req = CreateUpmRequest("updateLinks");

            var result = RunUpmAsync(req);
            yield return result;

            if (!result.Current.Succeeded)
            {
                DisplayUpmError("Updating Directory Links", result.Current.ErrorMessage);
                yield return false;
            }

            AssetDatabase.Refresh();
            Log.Info("Projeny: Updated directory links");
            yield return true;
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

            if (!result.Current.Succeeded)
            {
                DisplayUpmError("Installing Release '{0}' ({1})".Fmt(name, version), result.Current.ErrorMessage);
                yield return false;
            }

            Log.Info("Installed new release '{0}' ({1})".Fmt(name, version));
            yield return true;
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

        // This is called by the build script to generate the monodevelop solution
        // because it uses that when generating its own custom solution
        public static void UpdateMonodevelopProject()
        {
            EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
        }

        [DidReloadScripts]
        static void VerifyConfig()
        {
            VerifyThatAllDirectoriesAreJunctions();
            VerifyPlatformIsCorrect();
        }

        static void VerifyPlatformIsCorrect()
        {
            var info = GetCurrentProjectInfo();

            var expectedPlatform = FromPlatformDirStr(info.PlatformDirName);

            if (EditorUserBuildSettings.activeBuildTarget != expectedPlatform)
            {
                if (EditorUserBuildSettings.SwitchActiveBuildTarget(expectedPlatform))
                {
                    EditorUtility.DisplayDialog(
                        "Error", "Projeny has detected an unexpected platform change.\n\nPlatforms should only be changed through Projeny and never through Unity's normal Build Settings dialog.\n\nThis is necessary to allow Projeny to include platform specific packages, quick platform switching, etc.\n\nUPM has switched the platform back to '" + expectedPlatform.ToString() + "'", "Ok");
                }
                else
                {
                    UnityEngine.Debug.LogError("UPM - Unknown error occurred when attempting to switch platform to '" + expectedPlatform.ToString() + "'");
                }
            }
        }

        static void VerifyThatAllDirectoriesAreJunctions()
        {
            var badDirectories = new List<DirectoryInfo>();

            foreach (var scriptDir in new DirectoryInfo(Application.dataPath).GetDirectories())
            {
                if (scriptDir.Name == "Plugins")
                {
                    foreach (var pluginDir in scriptDir.GetDirectories())
                    {
                        if (pluginDir.Name != "Projeny" && !JunctionPoint.Exists(pluginDir.FullName))
                        {
                            badDirectories.Add(pluginDir);
                        }
                    }

                    continue;
                }

                if (!JunctionPoint.Exists(scriptDir.FullName))
                {
                    badDirectories.Add(scriptDir);
                }
            }

            if (badDirectories.Any())
            {
                var badDirectoriesStr = string.Join("\n", badDirectories.Select(x => "Assets/" + x.FullName.Substring(Application.dataPath.Length + 1)).ToArray());

                EditorUtility.DisplayDialog(
                    "Error", "Found some directories that were not created by Projeny.  This could cause data loss.  All user data in Projeny should reside in the UnityPackages directory. See documentation for details.  \n\nThe directories in question are the following: \n\n{0}".Fmt(badDirectoriesStr), "Ok");
            }
        }

        public static void ChangePlatform(BuildTarget desiredPlatform)
        {
            Assert.Throw("TODO");
            //if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            //{
                //// They hit cancel in the save dialog
                //return;
            //}

            //if (GetPlatformFromDirectoryName() == desiredPlatform)
            //{
                //UnityEngine.Debug.Log("Projeny: Already at the desired platform, no need to change project.");
                //return;
            //}

            //try
            //{
                //var req = new UpmRequest()
                //{
                    //RequestId = "updateLinks",
                    //ProjectName = GetCurrentProjectName(),
                    //Platform = desiredPlatform,
                    //ConfigPath = FindUpmConfigPath()
                //};

                //RunUpm(req);

                //req.RequestId = "openUnity";
                //RunUpm(req);
            //}
            //catch (UpmException e)
            //{
                //EditorUtility.DisplayDialog("Error", "Change platform failed with erros: \n" + e.Message, "Ok");
                //throw e;
            //}

            //EditorApplication.Exit(0);
        }

        static string GetCurrentProjectName()
        {
            return GetCurrentProjectInfo().ProjectName;
        }

        static string GetCurrentPlatformDirName()
        {
            return GetCurrentProjectInfo().PlatformDirName;
        }

        static CurrentProjectInfo GetCurrentProjectInfo()
        {
            var info = new CurrentProjectInfo();

            var projectPlatformRootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var projectRootPath = Path.GetFullPath(Path.Combine(projectPlatformRootPath, ".."));

            info.ProjectName = Path.GetFileName(projectRootPath);

            var projectAndPlatform = Path.GetFileName(projectPlatformRootPath);

            info.PlatformDirName = projectAndPlatform.Substring(projectAndPlatform.LastIndexOf("-")+1);

            return info;
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

        // NOTE: This needs to stay in sync with BuildUtil.py
        static BuildTarget FromPlatformDirStr(string platformShortStr)
        {
            switch (platformShortStr.ToLower())
            {
                case "windows":
                {
                    return BuildTarget.StandaloneWindows;
                }
                case "android":
                {
                    return BuildTarget.Android;
                }
                case "webplayer":
                {
                    return BuildTarget.WebPlayer;
                }
                case "webgl":
                {
                    return BuildTarget.WebGL;
                }
                case "osx":
                {
                    return BuildTarget.StandaloneOSXUniversal;
                }
                case "ios":
                {
                    return BuildTarget.iOS;
                }
                case "linux":
                {
                    return BuildTarget.StandaloneLinux;
                }
            }

            throw new NotImplementedException();
        }

        static BuildTarget GetPlatformFromDirectoryName()
        {
            return FromPlatformDirStr(GetCurrentPlatformDirName());
        }

        public static UpmRequest CreateUpmRequest(string requestId)
        {
            return new UpmRequest()
            {
                RequestId = requestId,
                ProjectName = GetCurrentProjectName(),
                Platform = GetPlatformFromDirectoryName(),
                ConfigPath = FindUpmConfigPath()
            };
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
            startInfo.Arguments = "\"{0}\" \"{1}\" {2} {3} {4} {5}"
                .Fmt(request.ConfigPath, request.ProjectName, ToPlatformDirStr(request.Platform), request.RequestId, request.Param1 ?? "", request.Param2 ?? "");

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
                var configPath = Path.Combine(dirInfo.FullName, ConfigFileName);

                if (File.Exists(configPath))
                {
                    return configPath;
                }
            }

            throw new UpmException(
                "Could not locate {0} when searching from {1} upwards".Fmt(ConfigFileName, Application.dataPath));
        }

        class CurrentProjectInfo
        {
            public string PlatformDirName;
            public string ProjectName;
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
