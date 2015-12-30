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
    // We need to make this ScriptableObject because it is referenced
    // using polymorphism in DraggableList (in the object tag field)
    // And polymorphism is only supported for ScriptableObject's
    [Serializable]
    public class ReleaseInfo : ScriptableObject
    {
        public string Title;
        public string Version;
        public string LocalPath;
    }
}


