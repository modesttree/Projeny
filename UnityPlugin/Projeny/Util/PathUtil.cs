using System;
using System.Collections.Generic;
using System.IO;

namespace ModestTree
{
    public static class PathUtil
    {
        public static bool IsSubPath(string parent, string child)
        {
            // call Path.GetFullPath to Make sure we're using Path.DirectorySeparatorChar
            parent = Path.GetFullPath(parent);
            child = Path.GetFullPath(child);

            return child.StartsWith(parent);
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

        public static string Combine(params string[] paths)
        {
            Assert.That(!paths.IsEmpty());

            string result = paths[0];

            for (int i = 1; i < paths.Length; i++)
            {
                result = Path.Combine(result, paths[i]);
            }

            return result;
        }

        public static string GetRelativePath(string fromDirectory, string toPath)
        {
            Assert.IsNotNull(toPath);
            Assert.IsNotNull(fromDirectory);

            // call Path.GetFullPath to Make sure we're using Path.DirectorySeparatorChar
            fromDirectory = Path.GetFullPath(fromDirectory);
            toPath = Path.GetFullPath(toPath);

            bool isRooted = (Path.IsPathRooted(fromDirectory) && Path.IsPathRooted(toPath));

            if (isRooted)
            {
                bool isDifferentRoot = (string.Compare(Path.GetPathRoot(fromDirectory), Path.GetPathRoot(toPath), true) != 0);

                if (isDifferentRoot)
                {
                    return toPath;
                }
            }

            List<string> relativePath = new List<string>();
            string[] fromDirectories = fromDirectory.Split(Path.DirectorySeparatorChar);

            string[] toDirectories = toPath.Split(Path.DirectorySeparatorChar);

            int length = Math.Min(fromDirectories.Length, toDirectories.Length);

            int lastCommonRoot = -1;

            // find common root
            for (int x = 0; x < length; x++)
            {
                if (string.Compare(fromDirectories[x], toDirectories[x], true) != 0)
                {
                    break;
                }

                lastCommonRoot = x;
            }

            if (lastCommonRoot == -1)
                return toPath;

            // add relative folders in from path
            for (int x = lastCommonRoot + 1; x < fromDirectories.Length; x++)
            {
                if (fromDirectories[x].Length > 0)
                {
                    relativePath.Add("..");
                }
            }

            // add to folders to path
            for (int x = lastCommonRoot + 1; x < toDirectories.Length; x++)
            {
                relativePath.Add(toDirectories[x]);
            }

            // create relative path
            string[] relativeParts = new string[relativePath.Count];
            relativePath.CopyTo(relativeParts, 0);

            return string.Join(Path.DirectorySeparatorChar.ToString(), relativeParts);
        }

        public static IEnumerable<string> GetAllFilesUnderDirectory(string path)
        {
            Assert.That(Directory.Exists(path));

            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);

            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorException(ex);
                }

                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    Log.ErrorException(ex);
                }

                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }

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

        public static string FindExePathFromEnvPath(string exe)
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

        // Returns null if can't find anything that's valid
        public static string FindClosestValidPath(string absolutePath)
        {
            if (!Path.IsPathRooted(absolutePath))
            {
                return null;
            }

            while (!Directory.Exists(absolutePath))
            {
                var parentInfo = Directory.GetParent(absolutePath);

                if (parentInfo == null)
                {
                    return null;
                }

                absolutePath = parentInfo.FullName;
            }

            return absolutePath;
        }
    }
}
