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
    public static class ProjenyConfigValidator
    {
        [DidReloadScripts]
        static void VerifyConfig()
        {
            VerifyThatAllDirectoriesAreJunctions();
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
    }
}

