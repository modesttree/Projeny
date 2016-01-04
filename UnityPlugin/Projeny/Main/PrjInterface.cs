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
    public class PrjResponse
    {
        public readonly bool Succeeded;
        public readonly string ErrorMessage;
        public readonly string Output;

        PrjResponse(
            bool succeeded, string errorMessage, string output)
        {
            Succeeded = succeeded;
            ErrorMessage = errorMessage;
            Output = output;
        }

        public static PrjResponse Error(string errorMessage)
        {
            return new PrjResponse(false, errorMessage, null);
        }

        public static PrjResponse Success(string output = null)
        {
            return new PrjResponse(true, null, output);
        }
    }

    public class PrjRequest
    {
        public string RequestId;
        public string ProjectName;
        public BuildTarget Platform;
        public string ConfigPath;
        public string Param1;
        public string Param2;
    }

    public static class PrjInterface
    {
        public static PrjRequest CreatePrjRequest(string requestId)
        {
            return CreatePrjRequestForProjectAndPlatform(
                requestId,
                ProjenyEditorUtil.GetCurrentProjectName(),
                ProjenyEditorUtil.GetPlatformFromDirectoryName());
        }

        public static PrjRequest CreatePrjRequestForProject(
            string requestId, string project)
        {
            return CreatePrjRequestForProjectAndPlatform(
                requestId,
                project,
                ProjenyEditorUtil.GetPlatformFromDirectoryName());
        }

        public static PrjRequest CreatePrjRequestForPlatform(
            string requestId, BuildTarget platform)
        {
            return CreatePrjRequestForProjectAndPlatform(
                requestId,
                ProjenyEditorUtil.GetCurrentProjectName(),
                platform);
        }

        public static PrjRequest CreatePrjRequestForProjectAndPlatform(
            string requestId, string projectName, BuildTarget platform)
        {
            return new PrjRequest()
            {
                RequestId = requestId,
                ProjectName = projectName,
                Platform = platform,
                ConfigPath = FindPrjConfigPath()
            };
        }

        static ProcessStartInfo GetPrjProcessStartInfo(PrjRequest request)
        {
            var startInfo = new ProcessStartInfo();

            startInfo.FileName = FindPrjExePath();

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

        public static PrjResponse RunPrj(PrjRequest request)
        {
            Process proc = new Process();
            proc.StartInfo = GetPrjProcessStartInfo(request);

            proc.Start();

            var errorLines = new List<string>();
            proc.ErrorDataReceived += (sender, outputArgs) => errorLines.Add(outputArgs.Data);

            var outputLines = new List<string>();
            proc.OutputDataReceived += (sender, outputArgs) => outputLines.Add(outputArgs.Data);

            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            proc.WaitForExit();

            return RunPrjCommonEnd(
                proc, errorLines.Join(Environment.NewLine));
        }

        // This will yield string values that contain some status message
        // until finally yielding a value of type PrjResponse with the final data
        public static IEnumerator RunPrjAsync(PrjRequest request)
        {
            Process proc = new Process();
            proc.StartInfo = GetPrjProcessStartInfo(request);

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

            yield return RunPrjCommonEnd(
                proc, errorLines.Join(Environment.NewLine));
        }

        static PrjResponse RunPrjCommonEnd(
            Process proc, string errorOutput)
        {
            // If it returns an error code, then assume that
            // the contents of STDERR are the error message to display
            // to the user
            // Otherwise, assume the contents of STDERR are the final output
            // data.  This can include things like serialized YAML
            if (proc.ExitCode != 0)
            {
                return PrjResponse.Error(errorOutput);
            }

            return PrjResponse.Success(errorOutput);
        }

        static string FindPrjExePath()
        {
            try
            {
                return PathUtil.FindExePath("PrjEditorApi.bat");
            }
            catch (FileNotFoundException)
            {
                throw new PrjException(
                    "Could not locate path to PRJ.bat.  Have you added 'projeny/Bin/Prj' to your environment PATH?  See documentation for details.");
            }
        }

        public static string FindPrjConfigPath()
        {
            foreach (var dirInfo in PathUtil.GetAllParentDirectories(Application.dataPath))
            {
                var configPath = Path.Combine(dirInfo.FullName, ProjenyEditorUtil.ConfigFileName);

                if (File.Exists(configPath))
                {
                    return configPath;
                }
            }

            throw new PrjException(
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

        public class PrjException : Exception
        {
            public PrjException(string errorMessage)
                : base(errorMessage)
            {
            }
        }
    }
}
