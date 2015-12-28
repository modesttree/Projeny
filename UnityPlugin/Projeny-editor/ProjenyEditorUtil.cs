using System;
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
    public static class ProjenyEditorUtil
    {
        public const string ConfigFileName = "upm.yaml";

        public const string ProjectConfigFileName = "project.yaml";
        public const string ProjectConfigUserFileName = "projectUser.yaml";

        [MenuItem("Projeny/Help...", false, 9)]
        public static void OpenHelp()
        {
            Application.OpenURL("https://github.com/modesttree/projeny");
        }

        //[MenuItem("Projeny/Open project.yaml", false, 1)]
        public static void OpenProjectFile()
        {
            var configPath = Path.Combine(Application.dataPath, "../../project.yaml");
            InternalEditorUtility.OpenFileAtLineExternal(configPath, 1);
        }

        [MenuItem("Projeny/Package Manager...", false, 1)]
        public static void OpenPackageManager()
        {
            var window = EditorWindow.GetWindow<PackageManagerWindow>();
            window.titleContent = new GUIContent("  Projeny", Resources.Load<Texture2D>("Projeny/Icon"));
        }

        //[MenuItem("Projeny/Update Links", false, 1)]
        public static void UpdateLinks()
        {
            try
            {
                RunUpm("updateLinks");
            }
            catch (UpmException e)
            {
                EditorUtility.DisplayDialog("Error", "Update custom solution failed with errors: \n\n" + e.Message, "Ok");
                return;
            }

            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("Projeny: Directory links have been updated");
        }

        [MenuItem("Projeny/Update C# Project", false, 6)]
        public static void UpdateCustomSolution()
        {
            try
            {
                RunUpm("updateCustomSolution");
            }
            catch (UpmException e)
            {
                EditorUtility.DisplayDialog("Error", "Update custom solution failed with errors: \n\n" + e.Message, "Ok");
                return;
            }

            UnityEngine.Debug.Log("Projeny: Custom solution has been updated");
        }

        //[MenuItem("Projeny/Custom Solution/Open", false, 6)]
        public static void OpenCustomSolution()
        {
            try
            {
                RunUpm("openCustomSolution");
            }
            catch (UpmException e)
            {
                EditorUtility.DisplayDialog("Error", "Opening custom solution failed with errors: \n\n" + e.Message, "Ok");
                return;
            }

            UnityEngine.Debug.Log("Projeny: Opened custom solution");
        }

        [MenuItem("Projeny/Change Platform/Windows", false, 7)]
        public static void ChangePlatformWin()
        {
            ChangePlatform(BuildTarget.StandaloneWindows);
        }

        [MenuItem("Projeny/Change Platform/Webplayer", false, 7)]
        public static void ChangePlatformWebplayer()
        {
            ChangePlatform(BuildTarget.WebPlayer);
        }

        [MenuItem("Projeny/Change Platform/Android", false, 7)]
        public static void ChangePlatformAndroid()
        {
            ChangePlatform(BuildTarget.Android);
        }

        [MenuItem("Projeny/Change Platform/Web GL", false, 7)]
        public static void ChangePlatformWebGL()
        {
            ChangePlatform(BuildTarget.WebGL);
        }

        [MenuItem("Projeny/Change Platform/OsX", false, 7)]
        public static void ChangePlatformOsX()
        {
            ChangePlatform(BuildTarget.StandaloneOSXUniversal);
        }

        [MenuItem("Projeny/Change Platform/Linux", false, 7)]
        public static void ChangePlatformLinux()
        {
            ChangePlatform(BuildTarget.StandaloneLinux);
        }

        [MenuItem("Projeny/Change Platform/iOS", false, 7)]
        public static void ChangePlatformIos()
        {
            ChangePlatform(BuildTarget.iOS);
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

        static void ChangePlatform(BuildTarget desiredPlatform)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // They hit cancel in the save dialog
                return;
            }

            if (GetPlatformFromDirectoryName() == desiredPlatform)
            {
                UnityEngine.Debug.Log("Projeny: Already at the desired platform, no need to change project.");
                return;
            }

            try
            {
                RunUpm("updateLinks", desiredPlatform);
                RunUpm("openUnity", desiredPlatform);
            }
            catch (UpmException e)
            {
                EditorUtility.DisplayDialog("Error", "Change platform failed with erros: \n" + e.Message, "Ok");
                return;
            }

            EditorApplication.Exit(0);
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

        public static string RunUpm(string requestId)
        {
            return RunUpm(requestId, GetCurrentProjectName());
        }

        public static string RunUpm(string requestId, string projectName)
        {
            return RunUpm(requestId, projectName, GetPlatformFromDirectoryName());
        }

        public static string RunUpm(string requestId, BuildTarget platform)
        {
            return RunUpm(requestId, GetCurrentProjectName(), platform);
        }

        public static string RunUpm(string requestId, string projectName, BuildTarget platform)
        {
            return RunUpm(requestId, projectName, platform, FindUpmConfigPath());
        }

        public static string RunUpm(string requestId, string projectName, BuildTarget platform, string configPath)
        {
            var startInfo = new ProcessStartInfo();

            startInfo.FileName = FindUpmExePath();
            startInfo.Arguments = "\"{0}\" \"{1}\" {2} {3}".Fmt(configPath, projectName, ToPlatformDirStr(platform), requestId);

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

            proc.Start();

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            bool succeeded = proc.ExitCode == 0;

            if (!succeeded)
            {
                throw new UpmException(allOutput.ToString());
            }

            return allOutput.ToString();
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
    }
}
