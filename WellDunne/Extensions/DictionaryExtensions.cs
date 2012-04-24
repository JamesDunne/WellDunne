using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public delegate bool TryParse<T>(string tmp, out T result);
}

namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Try to get the string value from the dictionary and then try to parse it with the given <paramref name="tryParse"/> delegate
        /// and return the parsed value upon success, otherwise return <paramref name="defaultValue"/> if the key was not found or if the
        /// parse failed.
        /// </summary>
        /// <typeparam name="TKey">Key type</typeparam>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="dict">Dictionary to look up key in</param>
        /// <param name="key">Key to look up</param>
        /// <param name="tryParse">Delegate to attempt to parse the string value into target type <typeparamref name="T"/></param>
        /// <param name="defaultValue">Thunk to yield a default value if the key is not found or if parsing fails. If this is null, default(<typeparamref name="T"/>) is used.</param>
        /// <returns></returns>
        public static T TryGetParse<TKey, T>(this IDictionary<TKey, string> dict, TKey key, TryParse<T> tryParse, Thunk<T> defaultValue)
        {
            if (tryParse == null) throw new ArgumentNullException("tryParse");
            if (defaultValue == null) defaultValue = default(T);
            if (dict == null) return (T)defaultValue;

            T result;
            string tmp;

            if (dict.TryGetValue(key, out tmp)
                && tryParse(tmp, out result))
                return result;

            return (T)defaultValue;
        }

        public static T GetValueOrDefault<TKey, T>(this IDictionary<TKey, T> dict, TKey key, Thunk<T> defaultValue)
        {
            if (defaultValue == null) defaultValue = default(T);
            if (dict == null) return (T)defaultValue;

            T result;

            if (dict.TryGetValue(key, out result))
                return result;

            return (T)defaultValue;
        }
    }
}
