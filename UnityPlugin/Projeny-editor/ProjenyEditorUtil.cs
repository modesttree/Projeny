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
    public static class ProjenyEditorUtil
    {
        public const string ConfigFileName = "upm.yaml";

        public const string ProjectConfigFileName = "project.yaml";
        public const string ProjectConfigUserFileName = "projectUser.yaml";

        public static string GetCurrentProjectName()
        {
            return GetCurrentProjectInfo().ProjectName;
        }

        public static string GetCurrentPlatformDirName()
        {
            return GetCurrentProjectInfo().PlatformDirName;
        }

        // This is called by the build script to generate the monodevelop solution
        // because it uses that when generating its own custom solution
        public static void UpdateMonodevelopProject()
        {
            EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
        }

        public static ProjectInfo GetCurrentProjectInfo()
        {
            var info = new ProjectInfo();

            var projectPlatformRootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var projectRootPath = Path.GetFullPath(Path.Combine(projectPlatformRootPath, ".."));

            info.ProjectName = Path.GetFileName(projectRootPath);

            var projectAndPlatform = Path.GetFileName(projectPlatformRootPath);

            info.PlatformDirName = projectAndPlatform.Substring(projectAndPlatform.LastIndexOf("-")+1);

            return info;
        }

        public static BuildTarget GetPlatformFromDirectoryName()
        {
            return FromPlatformDirStr(GetCurrentPlatformDirName());
        }

        // NOTE: This needs to stay in sync with BuildUtil.py
        public static BuildTarget FromPlatformDirStr(string platformShortStr)
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

        public class ProjectInfo
        {
            public string PlatformDirName;
            public string ProjectName;
        }
    }
}

