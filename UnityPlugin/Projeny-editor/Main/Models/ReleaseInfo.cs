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
        public string Id;
        public string Name;

        // We'd prefer to use int? here but unity doesn't serialize nullables
        public bool HasVersionCode;
        public int VersionCode;

        // Can be empty if package is not versioned
        public string Version;

        // This will be empty if the package was not pulled from a unity package on the local machine
        public string LocalPath;

        // This will be empty if the package was pulled from a local path
        public string Url;

        public bool HasCompressedSize;
        public int CompressedSize;

        public string FileModificationDate;
        public long FileModificationDateTicks;

        // Only non-empty if this release is pulled from the asset store
        public AssetStoreInfo AssetStoreInfo;
    }

    [Serializable]
    public class AssetStoreInfo
    {
        public string PublisherId;
        public string PublisherLabel;
        public string PublishNotes;
        public string CategoryId;
        public string CategoryLabel;
        public string UploadId;
        public string Description;
        public string PublishDate;
        public long PublishDateTicks;
        public string UnityVersion;
        public string LinkId;
        public string LinkType;
    }
}
