using System;
using System.Collections;
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
    public class UpmResponse
    {
        public readonly bool Succeeded;
        public readonly string ErrorMessage;
        public readonly string Output;

        UpmResponse(
            bool succeeded, string errorMessage, string output)
        {
            Succeeded = succeeded;
            ErrorMessage = errorMessage;
            Output = output;
        }

        public static UpmResponse Error(string errorMessage)
        {
            return new UpmResponse(false, errorMessage, null);
        }

        public static UpmResponse Success(string output = null)
        {
            return new UpmResponse(true, null, output);
        }
    }

    public class UpmRequest
    {
        public string RequestId;
        public string ProjectName;
        public BuildTarget Platform;
        public string ConfigPath;
        public string Param1;
        public string Param2;
    }

    public static class UpmInterface
    {
        public static UpmRequest CreateUpmRequest(string requestId)
        {
            return CreateUpmRequestForProjectAndPlatform(
                requestId,
                ProjenyEditorUtil.GetCurrentProjectName(),
                ProjenyEditorUtil.GetPlatformFromDirectoryName());
        }

        public static UpmRequest CreateUpmRequestForPlatform(
            string requestId, BuildTarget platform)
        {
            return CreateUpmRequestForProjectAndPlatform(
                requestId,
                ProjenyEditorUtil.GetCurrentProjectName(),
                platform);
        }

        public static UpmRequest CreateUpmRequestForProjectAndPlatform(
            string requestId, string projectName, BuildTarget platform)
        {
            return new UpmRequest()
            {
                RequestId = requestId,
                ProjectName = projectName,
                Platform = platform,
                ConfigPath = FindUpmConfigPath()
            };
        }

        static ProcessStartInfo GetUpmProcessStartInfo(UpmRequest request)
        {
            var startInfo = new ProcessStartInfo();

            startInfo.FileName = FindUpmExePath();

            var argStr = "\"{0}\" \"{1}\" {2} {3}"
                .Fmt(
                    request.ConfigPath, request.ProjectName,
                    ToPlatformDirStr(request.Platform), request.RequestId);

            if (request.Param1 != null)
            {
                argStr += " \"{0}\"".Fmt(request.Param1);
            }

            if (request.Param2 != null)
            {
                argStr += " \"{0}\"".Fmt(request.Param2);
            }

            startInfo.Arguments = argStr;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            Log.Debug("Running command '{0} {1}'".Fmt(startInfo.FileName, startInfo.Arguments));

            return startInfo;
        }

        public static UpmResponse RunUpm(UpmRequest request)
        {
            Process proc = new Process();
            proc.StartInfo = GetUpmProcessStartInfo(request);

            proc.Start();

            var errorLines = new List<string>();
            proc.ErrorDataReceived += (sender, outputArgs) => errorLines.Add(outputArgs.Data);

            var outputLines = new List<string>();
            proc.OutputDataReceived += (sender, outputArgs) => outputLines.Add(outputArgs.Data);

            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            proc.WaitForExit();

            return RunUpmCommonEnd(
                proc, errorLines.Join(Environment.NewLine));
        }

        // This will yield string values that contain some status message
        // until finally yielding a value of type UpmResponse with the final data
        public static IEnumerator RunUpmAsync(UpmRequest request)
        {
            Process proc = new Process();
            proc.StartInfo = GetUpmProcessStartInfo(request);

            proc.EnableRaisingEvents = true;

            bool hasExited = false;
            proc.Exited += delegate
            {
                hasExited = true;
            };

            proc.Start();

            var errorLines = new List<string>();
            proc.ErrorDataReceived += (sender, outputArgs) => errorLines.Add(outputArgs.Data);

            var outputLines = new List<string>();
            proc.OutputDataReceived += (sender, outputArgs) => outputLines.Add(outputArgs.Data);

            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            while (!hasExited)
            {
                if (outputLines.IsEmpty())
                {
                    yield return null;
                }
                else
                {
                    var newLines = outputLines.ToList();
                    outputLines.Clear();
                    yield return newLines;
                }
            }

            yield return RunUpmCommonEnd(
                proc, errorLines.Join(Environment.NewLine));
        }

        static UpmResponse RunUpmCommonEnd(
            Process proc, string errorOutput)
        {
            // If it returns an error code, then assume that
            // the contents of STDERR are the error message to display
            // to the user
            // Otherwise, assume the contents of STDERR are the final output
            // data.  This can include things like serialized YAML
            if (proc.ExitCode != 0)
            {
                return UpmResponse.Error(errorOutput);
            }

            return UpmResponse.Success(errorOutput);
        }

        static string FindUpmExePath()
        {
            try
            {
                return PathUtil.FindExePath("UpmEditorApi.bat");
            }
            catch (FileNotFoundException)
            {
                throw new UpmException(
                    "Could not locate path to UPM.  Have you added 'projeny/Bin/Upm' to your environment PATH?  See documentation for details.");
            }
        }

        public static string FindUpmConfigPath()
        {
            foreach (var dirInfo in PathUtil.GetAllParentDirectories(Application.dataPath))
            {
                var configPath = Path.Combine(dirInfo.FullName, ProjenyEditorUtil.ConfigFileName);

                if (File.Exists(configPath))
                {
                    return configPath;
                }
            }

            throw new UpmException(
                "Could not locate {0} when searching from {1} upwards".Fmt(ProjenyEditorUtil.ConfigFileName, Application.dataPath));
        }

        static string ToPlatformDirStr(BuildTarget platform)
        {
            switch (platform)
            {
                case BuildTarget.StandaloneWindows:
                {
                    return "windows";
                }
                case BuildTarget.Android:
                {
                    return "android";
                }
                case BuildTarget.WebPlayer:
                {
                    return "webplayer";
                }
                case BuildTarget.WebGL:
                {
                    return "webgl";
                }
                case BuildTarget.StandaloneOSXUniversal:
                {
                    return "osx";
                }
                case BuildTarget.iOS:
                {
                    return "ios";
                }
                case BuildTarget.StandaloneLinux:
                {
                    return "linux";
                }
            }

            throw new NotImplementedException();
        }

        public class UpmException : Exception
        {
            public UpmException(string errorMessage)
                : base(errorMessage)
            {
            }
        }
    }
}
