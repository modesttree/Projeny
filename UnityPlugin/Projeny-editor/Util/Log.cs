using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Projeny.Internal
{
    // Simple wrapper around unity's logging system
    public static class Log
    {
        // Strip out debug logs outside of unity
        [Conditional("UNITY_EDITOR")]
        public static void Debug(string message, params object[] args)
        {
        }

        /////////////

        public static void Info(string message, params object[] args)
        {
            UnityEngine.Debug.Log(message.FmtSafe(args));
        }

        /////////////

        public static void Warn(string message, params object[] args)
        {
            UnityEngine.Debug.LogWarning(message.FmtSafe(args));
        }

        /////////////

        public static void Trace(string message, params object[] args)
        {
            UnityEngine.Debug.Log(message.FmtSafe(args));
        }

        /////////////

        public static void ErrorException(Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }

        public static void ErrorException(string message, Exception e)
        {
            UnityEngine.Debug.LogError(message);
            UnityEngine.Debug.LogException(e);
        }

        public static void Error(string message, params object[] args)
        {
            UnityEngine.Debug.LogError(message.FmtSafe(args));
        }
    }
}

