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

namespace Projeny.Internal
{
    public class ProjenyConfigValidator : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            VerifyProjenyConfig();
        }

        //[MenuItem("Projeny/Verify Config")]
        public static void VerifyProjenyConfig()
        {
            VerifyThatAllDirectoriesAreValidJunctions();
            VerifyPlatformIsCorrect();
        }

        static void VerifyPlatformIsCorrect()
        {
            var info = ProjenyEditorUtil.GetCurrentProjectInfo();

            var expectedPlatform = ProjenyEditorUtil.FromPlatformDirStr(info.PlatformDirName);

            if (EditorUserBuildSettings.activeBuildTarget != expectedPlatform)
            {
                if (EditorUserBuildSettings.SwitchActiveBuildTarget(expectedPlatform))
                {
                    UnityEngine.Debug.LogError(
                        "Projeny has detected an unexpected platform change.\n\nPlatforms should only be changed through Projeny and never through Unity's normal Build Settings dialog.\n\nThis is necessary to allow Projeny to include platform specific packages, quick platform switching, etc.\n\nProjeny has switched the platform back to '" + expectedPlatform.ToString() + "'");
                }
                else
                {
                    UnityEngine.Debug.LogError("Projeny - Unknown error occurred when attempting to switch platform to '" + expectedPlatform.ToString() + "'");
                }
            }
        }

        static void VerifyThatAllDirectoriesAreValidJunctions()
        {
            var badDirectories = new List<DirectoryInfo>();
            var brokenJunctions  = new List<string>();

            foreach (var scriptDir in new DirectoryInfo(Application.dataPath).GetDirectories())
            {
                var scriptNameLowered = scriptDir.Name.ToLower();

                if (scriptNameLowered == "editor default resources" || scriptNameLowered == "gizmos" || scriptNameLowered == "streamingassets")
                {
                    // TODO: Verify subdirectories here
                    continue;
                }

                if (scriptNameLowered == "plugins")
                {
                    foreach (var pluginDir in scriptDir.GetDirectories())
                    {
                        var pluginNameLowered = pluginDir.Name.ToLower();

                        if (pluginNameLowered == "projeny" || pluginNameLowered == "projenygenerated")
                        {
                            continue;
                        }

                        if (pluginNameLowered == "android" || pluginNameLowered == "webgl")
                        {
                            foreach (var platformDir in pluginDir.GetDirectories())
                            {
                                CheckJunction(platformDir, badDirectories, brokenJunctions);
                            }

                            continue;
                        }

                        CheckJunction(pluginDir, badDirectories, brokenJunctions);
                    }

                    continue;
                }

                CheckJunction(scriptDir, badDirectories, brokenJunctions);
            }

            if (badDirectories.Any())
            {
                var badDirectoriesStr = string.Join("\n", badDirectories.Select(x => "Assets/" + x.FullName.Substring(Application.dataPath.Length + 1)).ToArray());

                UnityEngine.Debug.LogError(
                    "Projeny validation failed.\n\nThere are directories in your project that were not created by Projeny.  This could cause data loss.  All user data in Projeny should reside in the UnityPackages directory.  See documentation for details.  \n\nThe directories in question are the following: \n\n{0}".Fmt(badDirectoriesStr));
            }

            if (brokenJunctions.Any())
            {
                var brokenJunctionsStr = string.Join("\n", brokenJunctions.Select(x => "Assets/" + x.Substring(Application.dataPath.Length + 1)).ToArray());

                UnityEngine.Debug.LogError(
                    "Projeny validation failed.\n\nThere are broken directory links in your project.  You may have deleted a package without removing the package from the project.  You can fix this by entering package manager and removing the missing packages from your project. See documentation for details.  \n\nThe directories in question are the following: \n\n{0}".Fmt(brokenJunctionsStr));
            }
        }

        static void CheckJunction(DirectoryInfo dir, List<DirectoryInfo> badDirectories, List<string> brokenJunctions)
        {
            if (JunctionPoint.Exists(dir.FullName))
            {
                if (!Directory.Exists(JunctionPoint.GetTarget(dir.FullName)))
                {
                    brokenJunctions.Add(dir.FullName);
                }
            }
            else
            {
                badDirectories.Add(dir);
            }
        }
    }
}

