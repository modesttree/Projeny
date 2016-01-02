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
    public class PmProjectConfigDeserializationException : Exception
    {
        public PmProjectConfigDeserializationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class PmProjectHandler
    {
        readonly PmView _view;
        readonly PmModel _model;

        public PmProjectHandler(
            PmModel model,
            PmView view)
        {
            _view = view;
            _model = model;
        }

        public ProjectConfig GetProjectConfigFromLists()
        {
            var config = new ProjectConfig();

            config.Packages = _model.AssetItems.ToList();
            config.PackagesPlugins = _model.PluginItems.ToList();

            return config;
        }

        string GetSerializedProjectConfigFromLists()
        {
            return UpmSerializer.SerializeProjectConfig(GetProjectConfigFromLists());
        }

        public void OverwriteConfig()
        {
            File.WriteAllText(
                ProjenyEditorUtil.GetProjectConfigPath(_view.ProjectConfigType), GetSerializedProjectConfigFromLists());
        }

        public void ResetProject()
        {
            _model.ClearAssetItems();
            _model.ClearPluginItems();
        }

        ProjectConfig DeserializeProjectConfig(string configPath)
        {
            try
            {
                return UpmSerializer.DeserializeProjectConfig(File.ReadAllText(configPath));
            }
            catch (Exception e)
            {
                throw new Exception(
                    "Error while reading from '{0}': \n\n{1}".Fmt(Path.GetFileName(configPath), e.Message));
            }
        }

        public bool HasProjectConfigChanged()
        {
            var configPath = ProjenyEditorUtil.GetProjectConfigPath(_view.ProjectConfigType);

            var currentConfig = GetProjectConfigFromLists();

            if (!File.Exists(configPath))
            {
                return !currentConfig.Packages.IsEmpty() || !currentConfig.PackagesPlugins.IsEmpty();
            }

            ProjectConfig savedConfig;

            try
            {
                savedConfig = DeserializeProjectConfig(configPath);
            }
            catch (Exception e)
            {
                Log.ErrorException(e);
                // This happens if we have serialization errors
                // Just log the error then assume that the file is different in this case so that the user
                // has the option to overwrite
                return true;
            }

            if (savedConfig == null)
            {
                return !currentConfig.Packages.IsEmpty() || !currentConfig.PackagesPlugins.IsEmpty();
            }

            return !Enumerable.SequenceEqual(currentConfig.Packages.OrderBy(t => t), savedConfig.Packages.OrderBy(t => t))
                || !Enumerable.SequenceEqual(currentConfig.PackagesPlugins.OrderBy(t => t), savedConfig.PackagesPlugins.OrderBy(t => t));
        }

        public void RefreshProject()
        {
            var configPath = ProjenyEditorUtil.GetProjectConfigPath(_view.ProjectConfigType);

            if (!File.Exists(configPath))
            {
                ResetProject();
                return;
            }

            ProjectConfig savedConfig;

            try
            {
                savedConfig = DeserializeProjectConfig(configPath);
            }
            catch (Exception e)
                // This can happen if the file has yaml serialization errors
            {
                throw new PmProjectConfigDeserializationException(
                    "Found serialization errors when reading from '{0}'".Fmt(configPath), e);
            }

            // Null when file is empty
            if (savedConfig == null)
            {
                ResetProject();
            }
            else
            {
                PopulateModelFromConfig(savedConfig);
            }
        }

        void PopulateModelFromConfig(ProjectConfig config)
        {
            _model.ClearPluginItems();

            foreach (var name in config.PackagesPlugins)
            {
                _model.AddPluginItem(name);
            }

            _model.ClearAssetItems();

            foreach (var name in config.Packages)
            {
                _model.AddAssetItem(name);
            }
        }
    }
}


