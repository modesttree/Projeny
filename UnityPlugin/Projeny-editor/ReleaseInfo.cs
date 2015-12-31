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
        public string Name;

        // We'd prefer to use int? here but unity doesn't serialize nullables
        public bool HasVersionCode;
        public int VersionCode;

        // Can be null if package is not versioned
        public string Version;

        // This will be null if the package was not pulled from a unity package on the local machine
        public string LocalPath;

        public bool HasCompressedSize;
        public int CompressedSize;

        // Only non-null if this release is pulled from the asset store
        public AssetStoreInfo AssetStoreInfo;
    }

    [Serializable]
    public class AssetStoreInfo : ScriptableObject
    {
        public string PublisherId;
        public string PublisherLabel;
        public string PackageId;
        public string PublishNotes;
        public string CategoryId;
        public string CategoryLabel;
        public string UploadId;
        public string Description;
        public string PublishDate;
        public string UnityVersion;
        public string LinkId;
        public string LinkType;
    }
}


