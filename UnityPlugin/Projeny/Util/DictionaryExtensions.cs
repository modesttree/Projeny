using System;
using System.Collections.Generic;
using ModestTree.Util;

namespace ModestTree
{
    public static class DictionaryExtensions
    {
        public static TValue TryGetValue<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, TKey key)
            where TValue : class
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            return null;
        }

        public static TValue GetValue<TValue>(
            this IDictionary<string, object> dictionary, string key)
        {
            TValue value;
            bool success = dictionary.TryGetValue(key, out value);
            Assert.That(success, "Could not find value for key '{0}'", key);

            return value;
        }

        public static TValue GetValue<TValue>(
            this IDictionary<string, object> dictionary, string key, TValue defaultValue)
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            return defaultValue;
        }

        public static TValue TryGetValue<TValue>(
            this IDictionary<string, object> dictionary, string key)
            where TValue : class
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            return null;
        }

        public static bool TryGetValue<TKey, TValue>(
            this IDictionary<TKey, object> dictionary, TKey key, out TValue value)
        {
            object valueObject;

            if (dictionary.TryGetValue(key, out valueObject) && valueObject is TValue)
            {
                value = (TValue)valueObject;
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }
    }
}
