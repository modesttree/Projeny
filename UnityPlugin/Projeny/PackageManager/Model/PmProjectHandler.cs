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
        public event Action SavedConfigFile = delegate {};
        public event Action LoadedConfigFile = delegate {};

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

            config.ProjectSettingsPath = _model.ProjectSettingsPath;

            config.AssetsFolder.AddRange(_model.AssetItems);
            config.PluginsFolder.AddRange(_model.PluginItems);
            config.SolutionProjects.AddRange(_model.VsProjects);
            config.Prebuilt.AddRange(_model.PrebuiltProjects);
            config.SolutionFolders.AddRange(_model.VsSolutionFolders);
            config.PackageFolders.AddRange(_model.SavedPackageFolders);

            return config;
        }

        string GetSerializedProjectConfigFromLists()
        {
            return PrjSerializer.SerializeProjectConfig(GetProjectConfigFromLists());
        }

        public void OverwriteConfig()
        {
            File.WriteAllText(
                ProjenyEditorUtil.GetProjectConfigPath(_view.ProjectConfigType), GetSerializedProjectConfigFromLists());
            SavedConfigFile();
        }

        public void ResetProject()
        {
            _model.ProjectSettingsPath = null;
            _model.ClearAssetItems();
            _model.ClearPluginItems();
        }

        ProjectConfig DeserializeProjectConfig(string configPath)
        {
            try
            {
                return PrjSerializer.DeserializeProjectConfig(File.ReadAllText(configPath));
            }
            catch (Exception e)
            {
                throw new Exception(
                    "Error while reading from '{0}'".Fmt(Path.GetFileName(configPath)), e);
            }
        }

        public bool HasProjectConfigChanged()
        {
            var configPath = ProjenyEditorUtil.GetProjectConfigPath(_view.ProjectConfigType);

            var currentConfig = GetProjectConfigFromLists();

            if (!File.Exists(configPath))
            {
                return !currentConfig.AssetsFolder.IsEmpty() || !currentConfig.PluginsFolder.IsEmpty();
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
                return !currentConfig.AssetsFolder.IsEmpty() || !currentConfig.PluginsFolder.IsEmpty();
            }

            return !Enumerable.SequenceEqual(currentConfig.AssetsFolder.OrderBy(t => t), savedConfig.AssetsFolder.OrderBy(t => t))
                || !Enumerable.SequenceEqual(currentConfig.PluginsFolder.OrderBy(t => t), savedConfig.PluginsFolder.OrderBy(t => t));
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
                LoadedConfigFile();
            }
        }

        void PopulateModelFromConfig(ProjectConfig config)
        {
            _model.ProjectSettingsPath = config.ProjectSettingsPath;

            _model.ClearPluginItems();
            foreach (var name in config.PluginsFolder)
            {
                _model.AddPluginItem(name);
            }

            _model.ClearAssetItems();
            foreach (var name in config.AssetsFolder)
            {
                _model.AddAssetItem(name);
            }

            _model.ClearSolutionProjects();
            foreach (var name in config.SolutionProjects)
            {
                _model.AddVsProject(name);
            }

            _model.ClearPrebuiltProjects();
            foreach (var name in config.Prebuilt)
            {
                _model.AddPrebuilt(name);
            }

            _model.ClearSolutionFolders();
            foreach (var pair in config.SolutionFolders)
            {
                _model.AddSolutionFolder(pair.Key, pair.Value);
            }

            _model.ClearSavedPackageFolders();
            foreach (var pair in config.PackageFolders)
            {
                _model.AddSavedPackageFolder(pair);
            }
        }
    }
}


