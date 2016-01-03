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
    public class PackageInfo
    {
        public string Name;
        public string Path;

        public PackageInstallInfo InstallInfo;
    }

    [Serializable]
    public class PackageInstallInfo
    {
        public string InstallDate;
        public long InstallDateTicks;

        public ReleaseInfo ReleaseInfo;
    }
}

