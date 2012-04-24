using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace System
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Prepends items in <paramref name="prepend"/> to this enumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">current enumerable to prepend to</param>
        /// <param name="prepend">enumerable to prepend</param>
        /// <returns></returns>
        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> source, IEnumerable<T> prepend)
        {
            foreach (T item in prepend)
            {
                yield return item;
            }
            foreach (T item in source)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Checks if the target object is null. If not, the selected member's value is returned,
        /// otherwise <code>default(<typeparamref name="U"/>)</code> is returned.
        /// </summary>
        /// <typeparam name="T">Type of the object to check against null</typeparam>
        /// <typeparam name="U">Type of the object's member to select</typeparam>
        /// <param name="obj">Source object to check against null reference</param>
        /// <param name="memberAccess">Function to access the member (e.g. <code>x => x.MemberName</code>)</param>
        /// <returns></returns>
        /// <remarks>This extension method implements a would-be-nice-to-have operator in C#, generally
        /// referred to as the proposed <code>?.</code> operator.</remarks>
        public static U NullSafe<T, U>(this T obj, Func<T, U> memberAccess)
            where T : class
        {
            if (obj == null) return default(U);

            return memberAccess(obj);
        }

        /// <summary>
        /// Checks if the target object is null. If not, the selected member's value is returned,
        /// otherwise <paramref name="defaultValue"/> is returned.
        /// </summary>
        /// <typeparam name="T">Type of the object to check against null</typeparam>
        /// <typeparam name="U">Type of the object's member to select</typeparam>
        /// <param name="obj">Source object to check against null reference</param>
        /// <param name="memberAccess">Function to access the member (e.g. <code>x => x.MemberName</code>)</param>
        /// <param name="defaultValue">Default value to return if the object is null</param>
        /// <returns></returns>
        /// <remarks>This extension method implements a would-be-nice-to-have operator in C#, generally
        /// referred to as the proposed <code>?.</code> operator.</remarks>
        public static U NullSafe<T, U>(this T obj, Func<T, U> memberAccess, U defaultValue)
            where T : class
        {
            if (obj == null) return defaultValue;

            return memberAccess(obj);
        }

        /// <summary>
        /// Formats a nullable value as an empty string if it is null, otherwise uses default ToString() method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nullableValue"></param>
        /// <returns></returns>
        public static string ToEmptyIfNullString<T>(this Nullable<T> nullableValue)
            where T : struct
        {
            if (!nullableValue.HasValue) return String.Empty;

            return nullableValue.Value.ToString();
        }
    }
}
