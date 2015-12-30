using System;
using System.Collections.Generic;
using System.IO;

namespace Projeny.Internal
{
    public static class PathUtil
    {
        public static IEnumerable<DirectoryInfo> GetAllParentDirectories(string path)
        {
            //Assert.That(Directory.Exists(path));
            return GetAllParentDirectories(new DirectoryInfo(path));
        }

        public static IEnumerable<DirectoryInfo> GetAllParentDirectories(DirectoryInfo dirInfo)
        {
            if (dirInfo == null || dirInfo.Name == dirInfo.Root.Name)
            {
                yield break;
            }

            yield return dirInfo;

            foreach (var parent in GetAllParentDirectories(dirInfo.Parent))
            {
                yield return parent;
            }
        }

        public static string FindExePath(string exe)
        {
            exe = Environment.ExpandEnvironmentVariables(exe);

            if (!File.Exists(exe))
            {
                if (Path.GetDirectoryName(exe) == String.Empty)
                {
                    foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
                    {
                        string path = test.Trim();

                        if (!String.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exe)))
                        {
                            return Path.GetFullPath(path);
                        }
                    }
                }

                throw new FileNotFoundException(
                    new FileNotFoundException().Message, exe);
            }

            return Path.GetFullPath(exe);
        }

        public static void AssertPathIsValid(string path)
        {
            try
            {
                Path.GetDirectoryName(path);
            }
            catch (Exception e)
            {
                throw new ArgumentException(
                    "Invalid path '{0}'".Fmt(path, e));
            }
        }
    }
}
