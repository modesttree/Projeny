using System;
using System.Collections.Generic;
using System.Linq;

namespace Projeny.Internal
{
    public static class StringExtensions
    {
        // We'd prefer to use the name Format here but that conflicts with
        // the existing string.Format method
        public static string Fmt(this string s, params object[] args)
        {
            return String.Format(s, args);
        }

        public static string Truncate(this string s, int maxLength)
        {
            return s.Substring(0, Math.Min(s.Length, maxLength));
        }

        public static bool CaseInsensitiveEquals(this string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}

