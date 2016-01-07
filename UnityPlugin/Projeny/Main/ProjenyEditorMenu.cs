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
        [MenuItem("Projeny/Help...", false, 10)]
        public static void OpenHelp()
        {
            Application.OpenURL("https://github.com/modesttree/projeny");
        }

        [MenuItem("Projeny/Update Links", false, 2)]
        public static void UpdateLinks()
        {
            PrjHelper.UpdateLinks();
        }

        [MenuItem("Projeny/Package Manager...", false, 1)]
        public static void OpenPackageManager()
        {
            GetEditorWindow();
        }

        static PmWindow GetEditorWindow()
        {
            var window = EditorWindow.GetWindow<PmWindow>();
            window.titleContent = new GUIContent("  Projeny", Resources.Load<Texture2D>("Projeny/Icon"));
            return window;
        }

        [MenuItem("Projeny/Change Project/New...", false, 9)]
        public static void CreateNewProject()
        {
            GetEditorWindow().ShowCreateNewProjectPopup();
        }

        [MenuItem("Projeny/Open C# Project", false, 6)]
        public static void OpenCustomSolution()
        {
            UpdateCustomSolution();

            var response = PrjInterface.RunPrj(PrjInterface.CreatePrjRequest("openCustomSolution"));

            if (!response.Succeeded)
            {
                PrjHelper.DisplayPrjError(
                    "Opening C# Project", response.ErrorMessage);
            }
        }

        [MenuItem("Projeny/Update C# Project", false, 6)]
        public static void UpdateCustomSolution()
        {
            // Need to do this every time in case the user changes the defines in the player settings
            SyntaxTree.VisualStudio.Unity.Bridge.ProjectFilesGenerator.GenerateProject();

            var response = PrjInterface.RunPrj(PrjInterface.CreatePrjRequest("updateCustomSolution"));

            if (response.Succeeded)
            {
                Log.Info("Projeny: Custom solution has been updated");
            }
            else
            {
                PrjHelper.DisplayPrjError(
                    "Updating C# Project", response.ErrorMessage);
            }
        }

        [MenuItem("Projeny/Change Platform/Windows", false, 7)]
        public static void ChangePlatformWin()
        {
            PrjHelper.ChangePlatform(BuildTarget.StandaloneWindows);
        }

        [MenuItem("Projeny/Change Platform/Webplayer", false, 7)]
        public static void ChangePlatformWebplayer()
        {
            PrjHelper.ChangePlatform(BuildTarget.WebPlayer);
        }

        [MenuItem("Projeny/Change Platform/Android", false, 7)]
        public static void ChangePlatformAndroid()
        {
            PrjHelper.ChangePlatform(BuildTarget.Android);
        }

        [MenuItem("Projeny/Change Platform/Web GL", false, 7)]
        public static void ChangePlatformWebGL()
        {
            PrjHelper.ChangePlatform(BuildTarget.WebGL);
        }

        [MenuItem("Projeny/Change Platform/OsX", false, 7)]
        public static void ChangePlatformOsX()
        {
            PrjHelper.ChangePlatform(BuildTarget.StandaloneOSXUniversal);
        }

        [MenuItem("Projeny/Change Platform/Linux", false, 7)]
        public static void ChangePlatformLinux()
        {
            PrjHelper.ChangePlatform(BuildTarget.StandaloneLinux);
        }

        [MenuItem("Projeny/Change Platform/iOS", false, 7)]
        public static void ChangePlatformIos()
        {
            PrjHelper.ChangePlatform(BuildTarget.iOS);
        }
    }
}
