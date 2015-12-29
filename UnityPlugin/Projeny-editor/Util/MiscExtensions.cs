using System;
using System.Collections.Generic;
using System.Linq;

namespace Projeny.Internal
{
    public static class MiscExtensions
    {
        // We'd prefer to use the name Format here but that conflicts with
        // the existing string.Format method
        public static string Fmt(this string s, params object[] args)
        {
            return string.Format(s, args);
        }

        public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
        {
            return !enumerable.Any();
        }

        // This is like string.Format except it will print NULL instead of just
        // a blank character when a parameter is null
        public static string FmtSafe(this string format, params object[] args)
        {
            var fixedArgs = args.Select(x => x == null ? "NULL" : x).ToArray();

            try
            {
                format = string.Format(format, fixedArgs);
            }
            catch (FormatException)
            {
                // Ignore, just don't do format
            }

            return format;
        }

        public static string Join(this IEnumerable<string> values, string separator)
        {
            return string.Join(separator, values.ToArray());
        }

        // Most of the time when you call remove you always intend on removing something
        // so assert in that case
        public static void RemoveWithConfirm<T>(this IList<T> list, T item)
        {
            bool removed = list.Remove(item);
            Assert.That(removed);
        }

        public static void RemoveWithConfirm<T>(this LinkedList<T> list, T item)
        {
            bool removed = list.Remove(item);
            Assert.That(removed);
        }

        public static void RemoveWithConfirm<TKey, TVal>(this IDictionary<TKey, TVal> dictionary, TKey key)
        {
            bool removed = dictionary.Remove(key);
            Assert.That(removed);
        }

        public static void RemoveWithConfirm<T>(this HashSet<T> set, T item)
        {
            bool removed = set.Remove(item);
            Assert.That(removed);
        }
    }
}
