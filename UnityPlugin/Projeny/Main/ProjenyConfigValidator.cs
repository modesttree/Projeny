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
                    EditorUtility.DisplayDialog(
                        "Error", "Projeny has detected an unexpected platform change.\n\nPlatforms should only be changed through Projeny and never through Unity's normal Build Settings dialog.\n\nThis is necessary to allow Projeny to include platform specific packages, quick platform switching, etc.\n\nProjeny has switched the platform back to '" + expectedPlatform.ToString() + "'", "Ok");
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
                if (scriptDir.Name == "Plugins")
                {
                    foreach (var pluginDir in scriptDir.GetDirectories())
                    {
                        if (pluginDir.Name == "Projeny")
                        {
                            continue;
                        }

                        if (JunctionPoint.Exists(pluginDir.FullName))
                        {
                            if (!Directory.Exists(JunctionPoint.GetTarget(pluginDir.FullName)))
                            {
                                brokenJunctions.Add(pluginDir.FullName);
                            }
                        }
                        else
                        {
                            badDirectories.Add(pluginDir);
                        }
                    }

                    continue;
                }

                if (JunctionPoint.Exists(scriptDir.FullName))
                {
                    if (!Directory.Exists(JunctionPoint.GetTarget(scriptDir.FullName)))
                    {
                        brokenJunctions.Add(scriptDir.FullName);
                    }
                }
                else
                {
                    badDirectories.Add(scriptDir);
                }
            }

            if (badDirectories.Any())
            {
                var badDirectoriesStr = string.Join("\n", badDirectories.Select(x => "Assets/" + x.FullName.Substring(Application.dataPath.Length + 1)).ToArray());

                EditorUtility.DisplayDialog(
                    "Projeny Error", "Projeny validation failed.\n\nThere are directories in your project that were not created by Projeny.  This could cause data loss.  All user data in Projeny should reside in the UnityPackages directory.  See documentation for details.  \n\nThe directories in question are the following: \n\n{0}".Fmt(badDirectoriesStr), "Ok");
            }

            if (brokenJunctions.Any())
            {
                var brokenJunctionsStr = string.Join("\n", brokenJunctions.Select(x => "Assets/" + x.Substring(Application.dataPath.Length + 1)).ToArray());

                EditorUtility.DisplayDialog(
                    "Projeny Error", "Projeny validation failed.\n\nThere are broken directory links in your project.  You may have deleted a package without removing the package from the project.  You can fix this by entering package manager and removing the missing packages from your project. See documentation for details.  \n\nThe directories in question are the following: \n\n{0}".Fmt(brokenJunctionsStr), "Ok");
            }
        }
    }
}
