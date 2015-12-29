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
    public static class ProjenyEditorMenu
    {
        [MenuItem("Projeny/Help...", false, 9)]
        public static void OpenHelp()
        {
            Application.OpenURL("https://github.com/modesttree/projeny");
        }

        [MenuItem("Projeny/Update Links", false, 1)]
        public static void UpdateLinks()
        {
            UpmInterface.UpdateLinks();
        }

        [MenuItem("Projeny/Package Manager...", false, 1)]
        public static void OpenPackageManager()
        {
            var window = EditorWindow.GetWindow<PackageManagerWindow>();
            window.titleContent = new GUIContent("  Projeny", Resources.Load<Texture2D>("Projeny/Icon"));
        }

        [MenuItem("Projeny/Update C# Project", false, 6)]
        public static void UpdateCustomSolution()
        {
            var response = UpmInterface.RunUpm(UpmInterface.CreateUpmRequest("updateCustomSolution"));

            if (response.Succeeded)
            {
                Log.Info("Projeny: Custom solution has been updated");
            }
            else
            {
                UpmInterface.DisplayUpmError(
                    "Updating C# Project", response.ErrorMessage);
            }
        }

        [MenuItem("Projeny/Change Platform/Windows", false, 7)]
        public static void ChangePlatformWin()
        {
            UpmInterface.ChangePlatform(BuildTarget.StandaloneWindows);
        }

        [MenuItem("Projeny/Change Platform/Webplayer", false, 7)]
        public static void ChangePlatformWebplayer()
        {
            UpmInterface.ChangePlatform(BuildTarget.WebPlayer);
        }

        [MenuItem("Projeny/Change Platform/Android", false, 7)]
        public static void ChangePlatformAndroid()
        {
            UpmInterface.ChangePlatform(BuildTarget.Android);
        }

        [MenuItem("Projeny/Change Platform/Web GL", false, 7)]
        public static void ChangePlatformWebGL()
        {
            UpmInterface.ChangePlatform(BuildTarget.WebGL);
        }

        [MenuItem("Projeny/Change Platform/OsX", false, 7)]
        public static void ChangePlatformOsX()
        {
            UpmInterface.ChangePlatform(BuildTarget.StandaloneOSXUniversal);
        }

        [MenuItem("Projeny/Change Platform/Linux", false, 7)]
        public static void ChangePlatformLinux()
        {
            UpmInterface.ChangePlatform(BuildTarget.StandaloneLinux);
        }

        [MenuItem("Projeny/Change Platform/iOS", false, 7)]
        public static void ChangePlatformIos()
        {
            UpmInterface.ChangePlatform(BuildTarget.iOS);
        }
    }
}

