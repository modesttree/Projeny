using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Projeny.Internal;

namespace Projeny
{
    public static class PrjPathVars
    {
        static Dictionary<string, string> _varMap;

        static PrjPathVars()
        {
            var response = PrjInterface.RunPrj(PrjInterface.CreatePrjRequest("getPathVars"));

            if (response.Succeeded)
            {
                _varMap = YamlSerializer.Deserialize<Dictionary<string, string>>(response.Output);
            }
            else
            {
                PrjHelper.DisplayPrjError("Initializing Path Vars", response.ErrorMessage);
            }
        }

        public static string Expand(string value)
        {
            return Expand(value, new Dictionary<string, string>());
        }

        public static string Expand(
            string value, params Tuple<string, string>[] extraVars)
        {
            return Expand(value, extraVars.ToDictionary(x => x.First, x => x.Second));
        }

        public static string Expand(
            string value, Dictionary<string, string> extraVars)
        {
            Assert.IsNotNull(_varMap);
            string finalPath;

            try
            {
                finalPath = ExpandInternal(value, extraVars);
            }
            catch (Exception e)
            {
                Log.ErrorException("Error while expanding value '{0}'".Fmt(value), e);
                throw;
            }

            Assert.That(!finalPath.Contains("[") && !finalPath.Contains("]"), "Malformed value found '{0}'", finalPath);
            return finalPath;
        }

        static string ExpandInternal(string value, Dictionary<string, string> extraVars)
        {
            return Regex.Replace(value, "(\\[[^\\]]+\\])", (match) => GetPathReplacement(match, extraVars));
        }

        static string GetPathReplacement(Match match, Dictionary<string, string> extraVars)
        {
            var key = match.Value.Substring(1, match.Value.Length - 2);

            string resultValue;

            bool success = _varMap.TryGetValue(key, out resultValue);

            if (!success)
            {
                success = extraVars.TryGetValue(key, out resultValue);

                if (!success)
                {
                    resultValue = Environment.GetEnvironmentVariable(key);
                    success = (resultValue != null);
                }
            }

            Assert.That(success, "Could not find key '{0}'", key);

            return ExpandInternal(resultValue, extraVars);
        }
    }
}
