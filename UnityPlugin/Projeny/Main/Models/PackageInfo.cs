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
    [Serializable]
    public class PackageFolderInfo
    {
        public string Path;
        public List<PackageInfo> Packages = new List<PackageInfo>();
    }

    [Serializable]
    public class PackageInfo
    {
        public string Name;
        public PackageInstallInfo InstallInfo;
        public string FullPath;
    }

    [Serializable]
    public class PackageInstallInfo
    {
        public string InstallDate;
        public long InstallDateTicks;

        public ReleaseInfo ReleaseInfo;
    }
}

