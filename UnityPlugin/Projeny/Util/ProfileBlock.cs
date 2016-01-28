using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ModestTree.Util;
using System.Xml.Serialization;

#if UNITY3D
using UnityEngine;
#endif

namespace ModestTree.Util
{
    public class ProfileBlock : IDisposable
    {
#if PROFILING_ENABLED && UNITY3D
        static Regex _profilePattern;
        static bool _isActive;
        static bool _isEnabled;

        bool _rootBlock;

        public ProfileBlock(string sampleName, bool rootBlock)
        {
            Profiler.BeginSample(sampleName);
            _rootBlock = rootBlock;

            if (rootBlock)
            {
                Assert.That(!_isActive);
                _isActive = true;
            }
        }

        public ProfileBlock(string sampleName)
            : this(sampleName, false)
        {
        }

        static ProfileBlock()
        {
            try
            {
                var settings = Config.TryGetSettings<Settings>("ProfileBlock");

                if (settings != null)
                {
                    _isEnabled = settings.IsEnabled;

                    if (_isEnabled && settings.ApplyFilter)
                    {
                        _profilePattern = BuildRegex(settings.ProfilePatterns);
                    }

                    Log.Debug("Started ProfileBlock with settings 'IsEnabled = {0}, ApplyFilter = {1}'", settings.IsEnabled, settings.ApplyFilter);
                }
            }
            catch
            {
            }
        }

        static Regex BuildRegex(List<string> patterns)
        {
            Assert.That(_isEnabled);
            if (patterns.Count == 0)
            {
                return null;
            }

            string fullPattern = "";

            foreach (var pattern in patterns)
            {
                if (fullPattern.Length > 0)
                {
                    fullPattern += "|";
                }

                fullPattern += pattern;
            }

            return new Regex(fullPattern, RegexOptions.Singleline);
        }

        public static ProfileBlock Start()
        {
            if (!_isEnabled)
            {
                return null;
            }

            return StartInternal();
        }

        static ProfileBlock StartInternal()
        {
            if (!Application.isEditor)
            {
                return null;
            }

            return Start(new MtStackTrace().Frames[2].ShortMethodName);
        }

        public static ProfileBlock Start(string sampleNameFormat, object obj1, object obj2)
        {
            return Start(string.Format(sampleNameFormat, obj1, obj2));
        }

        public static ProfileBlock Start(string sampleNameFormat, object obj)
        {
            return Start(string.Format(sampleNameFormat, obj));
        }

        public static ProfileBlock Start(string sampleName)
        {
            if (!_isEnabled)
            {
                return null;
            }

            return StartInternal(sampleName);
        }

        static ProfileBlock StartInternal(string sampleName)
        {
            if (!Application.isEditor)
            {
                return null;
            }

            if (_profilePattern == null || _isActive)
            {
                return new ProfileBlock(sampleName);
            }

            if (_profilePattern.Match(sampleName).Success)
            {
                return new ProfileBlock(sampleName, true);
            }

            return null;
        }

        public void Dispose()
        {
            Assert.That(Application.isEditor);
            Profiler.EndSample();

            if (_rootBlock)
            {
                Assert.That(_isActive);
                _isActive = false;
            }
        }

        public class Settings
        {
            public bool IsEnabled;
            public bool ApplyFilter;

            [XmlArray]
            [XmlArrayItem(ElementName="Item")]
            public List<string> ProfilePatterns;
        }
#else
        public ProfileBlock(string sampleName, bool rootBlock)
        {
        }

        public ProfileBlock(string sampleName)
            : this(sampleName, false)
        {
        }

        public static ProfileBlock Start()
        {
            return null;
        }

        public static ProfileBlock Start(string sampleNameFormat, object obj1, object obj2)
        {
            return null;
        }

        public static ProfileBlock Start(string sampleNameFormat, object obj)
        {
            return null;
        }

        public static ProfileBlock Start(string sampleName)
        {
            return null;
        }

        public void Dispose()
        {
        }
#endif
    }
}
