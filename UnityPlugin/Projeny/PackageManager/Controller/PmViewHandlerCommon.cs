using System;
using System.IO;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Projeny.Internal;
using System.Linq;

namespace Projeny.Internal
{
    public static class PmViewHandlerCommon
    {
        public const string NotAvailableLabel = "N/A";

        public static void OpenInAssetStore(string linkType, string linkId)
        {
            var fullUrl = "https://www.assetstore.unity3d.com/#/{0}/{1}".Fmt(linkType, linkId);
            Application.OpenURL(fullUrl);
        }

        static string ConvertByteSizeToDisplayValue(long bytesLong)
        {
            Decimal kilobytes = Convert.ToDecimal(bytesLong) / 1024.0m;

            if (kilobytes < 1024.0m)
            {
                return string.Format("{0:0} kB", kilobytes);
            }

            Decimal megabytes = Convert.ToDecimal(kilobytes) / 1024.0m;

            if (megabytes < 1024.0m)
            {
                return string.Format("{0:0} MB", megabytes);
            }

            Decimal gigabytes = Convert.ToDecimal(megabytes) / 1024.0m;

            return string.Format("{0:0} GB", gigabytes);
        }

        public static void AddReleaseInfoMoreInfoRows(ReleaseInfo info, PmSettings.ReleaseInfoMoreInfoDialogProperties skin)
        {
            DrawMoreInfoRow(skin, "Name", info.Name);
            GUILayout.Space(skin.RowSpacing);
            DrawMoreInfoRow(skin, "Version", string.IsNullOrEmpty(info.Version) ? NotAvailableLabel : info.Version);
            GUILayout.Space(skin.RowSpacing);
            DrawMoreInfoRow(skin, "Release Date", !string.IsNullOrEmpty(info.AssetStoreInfo.PublishDate) ? info.AssetStoreInfo.PublishDate : NotAvailableLabel);
            GUILayout.Space(skin.RowSpacing);
            DrawMoreInfoRow(skin, "Modification Date", !string.IsNullOrEmpty(info.FileModificationDate) ? info.FileModificationDate : NotAvailableLabel);
            GUILayout.Space(skin.RowSpacing);
            DrawMoreInfoRow(skin, "Compressed Size", info.HasCompressedSize ? ConvertByteSizeToDisplayValue(info.CompressedSize) : NotAvailableLabel);
            GUILayout.Space(skin.RowSpacing);
            DrawMoreInfoRow(skin, "Publisher", !string.IsNullOrEmpty(info.AssetStoreInfo.PublisherLabel) ? info.AssetStoreInfo.PublisherLabel : NotAvailableLabel);
            GUILayout.Space(skin.RowSpacing);
            DrawMoreInfoRow(skin, "Category", !string.IsNullOrEmpty(info.AssetStoreInfo.CategoryLabel) ? info.AssetStoreInfo.CategoryLabel : NotAvailableLabel);
            GUILayout.Space(skin.RowSpacing);
            DrawMoreInfoRow(skin, "Description", !string.IsNullOrEmpty(info.AssetStoreInfo.Description) ? info.AssetStoreInfo.Description : NotAvailableLabel);
            GUILayout.Space(skin.RowSpacing);
            DrawMoreInfoRow(skin, "Unity Version", !string.IsNullOrEmpty(info.AssetStoreInfo.UnityVersion) ? info.AssetStoreInfo.UnityVersion : NotAvailableLabel);
            GUILayout.Space(skin.RowSpacing);
            DrawMoreInfoRow(skin, "ID", info.Id);
            GUILayout.Space(skin.RowSpacing);
            DrawMoreInfoRow(skin, "Source URL", string.IsNullOrEmpty(info.Url) ? NotAvailableLabel : info.Url);
            GUILayout.Space(skin.RowSpacing);
            DrawMoreInfoRow(skin, "Publish Notes", !string.IsNullOrEmpty(info.AssetStoreInfo.PublishNotes) ? info.AssetStoreInfo.PublishNotes : NotAvailableLabel);
            GUILayout.Space(skin.RowSpacing);
            DrawMoreInfoRow(skin, "Version Code", info.HasVersionCode ? info.VersionCode.ToString() : NotAvailableLabel);
            GUILayout.Space(skin.RowSpacing);
        }

        public static void DrawMoreInfoRow(PmSettings.ReleaseInfoMoreInfoDialogProperties skin, string label, string value)
        {
            GUILayout.BeginHorizontal();
            {
                if (value == NotAvailableLabel)
                {
                    GUI.color = skin.NotAvailableColor;
                }
                GUILayout.Label(label + ":", skin.LabelStyle, GUILayout.Width(skin.LabelColumnWidth));
                GUILayout.Space(skin.ColumnSpacing);
                GUILayout.Label(value, skin.ValueStyle, GUILayout.Width(skin.ValueColumnWidth));
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();
        }
    }
}

