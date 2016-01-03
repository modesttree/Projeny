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
    public class PmInputHandler
    {
        readonly AsyncProcessor _asyncProcessor;
        readonly PmPackageHandler _packageHandler;
        readonly PmModel _model;
        readonly PmView _view;

        public PmInputHandler(
            PmView view,
            PmModel model,
            PmPackageHandler packageHandler,
            AsyncProcessor asyncProcessor)
        {
            _asyncProcessor = asyncProcessor;
            _packageHandler = packageHandler;
            _model = model;
            _view = view;
        }

        void DeleteSelected()
        {
            foreach (var group in _view.GetSelected().GroupBy(x => x.ListType))
            {
                switch (group.Key)
                {
                    case ListTypes.AssetItem:
                    {
                        foreach (var entry in group)
                        {
                            _model.RemoveAssetItem((string)entry.Model);
                        }
                        break;
                    }
                    case ListTypes.PluginItem:
                    {
                        foreach (var entry in group)
                        {
                            _model.RemovePluginItem((string)entry.Model);
                        }
                        break;
                    }
                    case ListTypes.Package:
                    {
                        _asyncProcessor.Process(
                            _packageHandler.DeletePackages(group.Select(x => (PackageInfo)x.Model).ToList()), "Deleting Packages");
                        break;
                    }
                }
            }
        }

        void SelectAll()
        {
            var selected = _view.GetSelected();

            if (!selected.IsEmpty())
            {
                var listType = selected[0].ListType;

                Assert.That(selected.All(x => x.ListType == listType));

                _view.GetList(listType).SelectAll();
            }
        }

        public void CheckForKeypresses()
        {
            if (_view.IsBlocked)
            {
                return;
            }

            var e = Event.current;

            if (e.type == EventType.ValidateCommand)
            {
                if (e.commandName == "SelectAll")
                {
                    e.Use();
                }
            }
            else if (e.type == EventType.ExecuteCommand)
            {
                if (e.commandName == "SelectAll")
                {
                    SelectAll();
                    e.Use();
                }
            }
            else if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.Delete:
                    {
                        DeleteSelected();
                        e.Use();
                        break;
                    }
                }
            }
        }
    }
}
