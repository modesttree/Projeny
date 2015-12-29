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
    public class ReleaseInfo
    {
        public string Title
        {
            get;
            set;
        }

        public string Version
        {
            get;
            set;
        }
    }
}


