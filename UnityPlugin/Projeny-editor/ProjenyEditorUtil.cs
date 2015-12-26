using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        [MenuItem("Projeny/Help...", false, 9)]
        public static void OpenHelp()
        {
            Application.OpenURL("https://github.com/modesttree/projeny");
        }

        [MenuItem("Projeny/Open project.yaml", false, 1)]
        public static void OpenProjectFile()
        {
            var configPath = Path.Combine(Application.dataPath, "../../project.yaml");
            InternalEditorUtility.OpenFileAtLineExternal(configPath, 1);
        }

        [MenuItem("Projeny/Update Links", false, 1)]
        public static void UpdateLinks()
        {
            if (RunUpmWithCurrentProjectAndPlatform("--updateLinks"))
            {
                AssetDatabase.Refresh();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Update custom solution failed.  An error occurred while executing UPM.bat from the command line.  See ProjenyLog.txt for details.", "Ok");
            }
        }

        [MenuItem("Projeny/Custom Solution/Update", false, 6)]
        public static void UpdateCustomSolution()
        {
            if (RunUpmWithCurrentProjectAndPlatform("--updateCustomSolution"))
            {
                //UnityEngine.Debug.Log("Projeny: Custom solution has been updated successfully");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Update custom solution failed.  An error occurred while executing UPM.bat from the command line.  See ProjenyLog.txt for details.", "Ok");
            }
        }

        [MenuItem("Projeny/Custom Solution/Open", false, 6)]
        public static void OpenCustomSolution()
        {
            if (RunUpmWithCurrentProjectAndPlatform("--openCustomSolution"))
            {
                UnityEngine.Debug.Log("Projeny: Opened custom solution");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Opening custom solution failed.  An error occurred while executing UPM.bat from the command line.  See ProjenyLog.txt for details.", "Ok");
            }
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

            if (RunUpmWithCurrentProject("--platform {0} --updateLinks --openUnity".Fmt(ToPlatformArgStr(desiredPlatform))))
            {
                EditorApplication.Exit(0);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Change platform failed.  An error occurred while executing UPM.bat from the command line.  See ProjenyLog.txt for details.", "Ok");
            }
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

        // NOTE: This needs to stay in sync with BuildUtil.py
        static string ToPlatformArgStr(BuildTarget desiredPlatform)
        {
            switch (desiredPlatform)
            {
                case BuildTarget.StandaloneWindows:
                {
                    return "win";
                }
                case BuildTarget.Android:
                {
                    return "and";
                }
                case BuildTarget.WebPlayer:
                {
                    return "webp";
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
                    return "lin";
                }
            }

            throw new NotImplementedException();
        }

        static bool RunUpmWithCurrentProjectAndPlatform(string args)
        {
            return RunUpmWithCurrentProject(
                "--platform {0} {1}".Fmt(ToPlatformArgStr(GetPlatformFromDirectoryName()), args));
        }

        static BuildTarget GetPlatformFromDirectoryName()
        {
            return FromPlatformDirStr(GetCurrentPlatformDirName());
        }

        static bool RunUpmWithCurrentProject(string args)
        {
            return RunUpm("--project {0} {1}".Fmt(GetCurrentProjectName(), args));
        }

        static bool RunUpm(string args)
        {
            Process proc = new Process();

            proc.StartInfo.FileName = "Upm";
            proc.StartInfo.Arguments = args;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;

            //UnityEngine.Debug.Log("Running command '{0} {1}'".Fmt(proc.StartInfo.FileName, args));

            proc.Start();
            proc.WaitForExit();

            return proc.ExitCode == 0;
        }

        class CurrentProjectInfo
        {
            public string PlatformDirName;
            public string ProjectName;
        }
    }
}
