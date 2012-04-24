using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace System.Collections.Generic
{
    public static class ListExtensions
    {
        public static void AddAllWhere<TValue>(this List<TValue> list, List<TValue> source, Predicate<TValue> match)
        {
            list.AddRange(source.FindAll(match));
        }

        private static IEnumerable<T1> preserveOrderOf<T1, T2>(IList<T1> set, IList<T2> ordered, Func<T1, T2, bool> compare, bool preserveOrdinal)
        {
            if (preserveOrdinal)
            {
                if (ordered.Count != set.Count) throw new ArgumentException("Cannot preserve ordinals if set count and ordered count do not match");
            }

            bool[] used = new bool[set.Count];

            // Move forward according to the order of `ordered`:
            for (int i = 0; i < ordered.Count; ++i)
            {
                // Try to find a key match in the set:
                bool match = false;
                for (int j = 0; j < set.Count; ++j)
                {
                    // Move on if this set value has been used before so that
                    // we don't keep using the first duplicate set value for duplicates:
                    if (used[j]) continue;

                    // The keys must compare:
                    if (compare(set[j], ordered[i]))
                    {
                        // Mark the set value as used and indicate a match:
                        used[j] = true;
                        match = true;
                        // Yield the set value:
                        yield return set[j];
                        // Don't produce duplicates in this output slot:
                        break;
                    }
                }

                // Preserve the output set size by using default(T1) as a placeholder
                // for missing values:
                if (preserveOrdinal && !match)
                    yield return default(T1);
            }

            // If we're not interested in preserving the ordinal positions, then
            // append the remaining unused set values:
            if (!preserveOrdinal)
            {
                for (int j = 0; j < set.Count; ++j)
                {
                    if (used[j]) continue;
                    yield return set[j];
                }
            }
            yield break;
        }

        /// <summary>
        /// Preserves this set's ordering according to the order of keys in the <paramref name="ordered"/> set.
        /// Any items found in this set that are not in <paramref name="ordered"/> will be appended to the end
        /// in the order that they appear.
        /// </summary>
        /// <typeparam name="T">The type of objects contained in the set</typeparam>
        /// <typeparam name="TKey">The type of key from each object to compare for ordering</typeparam>
        /// <param name="set">The set of items to reorder</param>
        /// <param name="ordered">The set of keys whose ordering will be preserved</param>
        /// <param name="keySelector">A lambda to select keys for comparison between the two sets to determine the ordering</param>
        /// <returns></returns>
        public static IEnumerable<T> PreserveOrderOf<T, TKey>(this IList<T> set, IList<T> ordered, Func<T, TKey> keySelector)
            where TKey : IEquatable<TKey>
        {
            var q = preserveOrderOf<T, T>(set, ordered, (x, y) => keySelector(x).Equals(keySelector(y)), false);
            foreach (var t in q)
            {
                yield return t;
            }
        }

        /// <summary>
        /// Preserves this set's ordering according to the order of keys in the <paramref name="ordered"/> set.
        /// Any items found in this set that are not in <paramref name="ordered"/> will be appended to the end
        /// in the order that they appear.
        /// </summary>
        /// <remarks>
        /// Duplicate keys are ordered in the order in which they appear in the set, which may or may not be
        /// accurate, but there is no way to guarantee same ordering in the face of duplicates.
        /// </remarks>
        /// <typeparam name="T">The type of objects contained in the set</typeparam>
        /// <typeparam name="TKey">The type of key from each object to compare for ordering</typeparam>
        /// <param name="set">The set of items to reorder</param>
        /// <param name="ordered">The set of keys whose ordering will be preserved</param>
        /// <param name="keySelector">A lambda to select keys for comparison between the two sets to determine the ordering</param>
        /// <returns></returns>
        public static IEnumerable<T> PreserveOrderOfKeys<T, TKey>(this IList<T> set, IList<TKey> ordered, Func<T, TKey> keySelector)
            where TKey : IEquatable<TKey>
        {
            var q = preserveOrderOf<T, TKey>(set, ordered, (x, y) => y.Equals(keySelector(x)), false);
            foreach (var t in q)
            {
                yield return t;
            }
        }

        /// <summary>
        /// Assuming this set and <paramref name="ordered"/> are equivalent sets, produce an output
        /// set containing items from this set using the key ordering from <paramref name="ordered"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects contained in the set</typeparam>
        /// <typeparam name="TKey">The type of key from each object to compare for ordering</typeparam>
        /// <param name="set">The set of items to reorder</param>
        /// <param name="ordered">The set of keys whose ordering will be preserved</param>
        /// <param name="keySelector">A lambda to select keys for comparison between the two sets to determine the ordering</param>
        /// <returns></returns>
        public static IEnumerable<T> EquivalenceSetOrder<T, TKey>(this IList<T> set, IList<T> ordered, Func<T, TKey> keySelector)
            where TKey : IEquatable<TKey>
        {
            var q = preserveOrderOf<T, T>(set, ordered, (x, y) => keySelector(y).Equals(keySelector(x)), true);
            foreach (var t in q)
            {
                yield return t;
            }
        }

        /// <summary>
        /// Assuming this set and <paramref name="ordered"/> are equivalent sets, produce an output
        /// set containing items from this set using the key ordering from <paramref name="ordered"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects contained in the set</typeparam>
        /// <typeparam name="TKey">The type of key from each object to compare for ordering</typeparam>
        /// <param name="set">The set of items to reorder</param>
        /// <param name="ordered">The set of keys whose ordering will be preserved</param>
        /// <param name="keySelector">A lambda to select keys for comparison between the two sets to determine the ordering</param>
        /// <returns></returns>
        public static IEnumerable<T> EquivalenceSetOrderKeys<T, TKey>(this IList<T> set, IList<TKey> ordered, Func<T, TKey> keySelector)
            where TKey : IEquatable<TKey>
        {
            var q = preserveOrderOf<T, TKey>(set, ordered, (x, y) => y.Equals(keySelector(x)), true);
            foreach (var t in q)
            {
                yield return t;
            }
        }

        /// <summary>
        /// Sets all of the key/value pairs from <paramref name="items"/> into the current dictionary type.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="items"></param>
        public static void MergeFrom<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            foreach (var item in items)
            {
                dict[item.Key] = item.Value;
            }
        }

        /// <summary>
        /// Creates a delimited string out of an IEnumerable source. <typeparamref name="T"/>.ToString() is used to format the items as strings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source IEnumerable to convert to a delimited string</param>
        /// <param name="delimiter">The delimiter used to delimit the items from the source IEnumerable, such as ",".</param>
        /// <returns></returns>
        public static string ToDelimitedString<T>(this IEnumerable<T> source, string delimiter)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (delimiter == null) throw new ArgumentNullException("delimiter");
            StringBuilder sb = new StringBuilder();
            using (var en = source.GetEnumerator())
            {
                bool notdone = en.MoveNext();
                while (notdone)
                {
                    sb.Append(en.Current.ToString());
                    notdone = en.MoveNext();
                    if (notdone) sb.Append(delimiter);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Creates a comma delimited string out of an IEnumerable source. <typeparamref name="T"/>.ToString() is used to format the items as strings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source IEnumerable to convert to a delimited string</param>
        /// <returns></returns>
        public static string ToCommaDelimitedString<T>(this IEnumerable<T> source)
        {
            return source.ToDelimitedString(",");
        }

        /// <summary>
        /// Creates a delimited string out of an IEnumerable source using a converter delegate to format the items as strings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source IEnumerable to convert to a delimited string</param>
        /// <param name="delimiter">The delimiter used to delimit the items from the source IEnumerable, such as ",".</param>
        /// <param name="converter">The converter delegate used to convert the <typeparamref name="T"/> into a string</param>
        /// <returns></returns>
        public static string ToDelimitedString<T>(this IEnumerable<T> source, string delimiter, Func<T, string> converter)
        {
            StringBuilder sb = new StringBuilder();
            using (var en = source.GetEnumerator())
            {
                bool notdone = en.MoveNext();
                while (notdone)
                {
                    sb.Append(converter(en.Current));
                    notdone = en.MoveNext();
                    if (notdone) sb.Append(delimiter);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Creates a comma delimited string out of an IEnumerable source using a converter delegate to format the items as strings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source IEnumerable to convert to a delimited string</param>
        /// <param name="converter">The converter delegate used to convert the <typeparamref name="T"/> into a string</param>
        /// <returns></returns>
        public static string ToCommaDelimitedString<T>(this IEnumerable<T> source, Func<T, string> converter)
        {
            return source.ToDelimitedString(",", converter);
        }

        /// <summary>
        /// If the source enumerable is a null reference, Enumerable.Empty() is returned. Otherwise, the original source is returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<T> NullCoalesceAsEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null) return Enumerable.Empty<T>();
            return source;
        }

        public static TResult[] SelectAsArray<T, TResult>(this T[] arr, Func<T, TResult> projection)
        {
            TResult[] res = new TResult[arr.Length];
            for (int i = 0; i < arr.Length; ++i)
                res[i] = projection(arr[i]);
            return res;
        }

        public static TResult[] SelectAsArray<T, TResult>(this ICollection<T> list, Func<T, TResult> projection)
        {
            TResult[] res = new TResult[list.Count];
            using (var en = list.GetEnumerator())
                for (int i = 0; en.MoveNext(); ++i)
                    res[i] = projection(en.Current);
            return res;
        }

        public static List<TResult> SelectAsList<T, TResult>(this ICollection<T> list, Func<T, TResult> projection)
        {
            List<TResult> res = new List<TResult>(list.Count);
            res.AddRange(list.Select(projection));
            return res;
        }

        /// <summary>
        /// Turns an ordered collection of denormalized grouped records into a collection of normalized groupings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TGroupKey"></typeparam>
        /// <typeparam name="TGroupValue"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="source">An ordered set of data ordered by the group key</param>
        /// <param name="getGroupKey">Gets the group key from the current record used to differentiate between groups</param>
        /// <param name="getGroupValue">Gets an instance per each group to represent the grouping key and its related data if any</param>
        /// <param name="selectGroup">Gets an instance to pair a group with its data</param>
        /// <returns></returns>
        public static IEnumerable<TResult> SelectFromOrderedGroups<T, TGroupKey, TGroupValue, TResult>(
            this IEnumerable<T> source,
            Func<T, TGroupKey> getGroupKey,
            Func<T, TGroupValue> getGroupValue,
            Func<TGroupValue, List<T>, TResult> selectGroup
        )
        {
            return SelectFromOrderedGroups(source, getGroupKey, null, getGroupValue, selectGroup);
        }

        /// <summary>
        /// Turns an ordered collection of denormalized grouped records into a collection of normalized groupings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TGroupKey"></typeparam>
        /// <typeparam name="TGroupValue"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="source">An ordered set of data ordered by the group key</param>
        /// <param name="getGroupKey">Gets the group key from the current record used to differentiate between groups</param>
        /// <param name="groupKeyComparer">An optional function used to compare group keys for equality</param>
        /// <param name="getGroupValue">Gets an instance per each group to represent the grouping key and its related data if any</param>
        /// <param name="selectGroup">Gets an instance to pair a group with its data</param>
        /// <returns></returns>
        public static IEnumerable<TResult> SelectFromOrderedGroups<T, TGroupKey, TGroupValue, TResult>(
            this IEnumerable<T> source,
            Func<T, TGroupKey> getGroupKey,
            Func<TGroupKey, TGroupKey, bool> groupKeyComparer,
            Func<T, TGroupValue> getGroupValue,
            Func<TGroupValue, List<T>, TResult> selectGroup
        )
        {
            if (getGroupKey == null) throw new ArgumentNullException("getGroupKey");
            if (getGroupValue == null) throw new ArgumentNullException("getGroupValue");
            if (selectGroup == null) throw new ArgumentNullException("selectGroup");
            if (source == null) yield break;

            Maybe<TGroupKey> lastKey = Maybe<TGroupKey>.Nothing;
            Maybe<TGroupValue> lastValue = Maybe<TGroupValue>.Nothing;
            List<T> range = new List<T>();

            var keyEq = groupKeyComparer;
            if (keyEq == null)
            {
                keyEq = (a, b) => EqualityComparer<TGroupKey>.Default.Equals(a, b);
            }

            using (var en = source.GetEnumerator())
                while (en.MoveNext())
                {
                    T curr = en.Current;

                    var currKey = getGroupKey(curr);
                    if (!lastKey.HasValue)
                    {
                        // First element has no previous element to compare to:
                        lastValue = getGroupValue(curr);
                    }
                    else if (!keyEq(lastKey.Value, currKey))
                    {
                        // Yield the last group produced:
                        yield return selectGroup(lastValue.Value, range);

                        // Start a new group:
                        range = new List<T>();
                        lastValue = getGroupValue(curr);
                    }

                    // This element goes to the current group:
                    range.Add(curr);
                    lastKey = currKey;
                }

            // Any elements for the last group?
            if (lastKey.HasValue) yield return selectGroup(lastValue.Value, range);
            yield break;
        }

        /// <summary>
        /// Turns an ordered collection of denormalized grouped records into a collection of normalized groupings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TGroupKey"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="source">An ordered set of data ordered by the group key</param>
        /// <param name="getGroupKey">Gets the group key from the current record used to differentiate between groups</param>
        /// <param name="selectGroup">Gets an instance to pair a group with its data</param>
        /// <returns></returns>
        public static IEnumerable<TResult> SelectFromOrderedGroups<T, TGroupKey, TResult>(
            this IEnumerable<T> source,
            Func<T, TGroupKey> getGroupKey,
            Func<TGroupKey, List<T>, TResult> selectGroup
        )
        {
            return SelectFromOrderedGroups(source, getGroupKey, (Func<TGroupKey, TGroupKey, bool>)null, selectGroup);
        }

        /// <summary>
        /// Turns an ordered collection of denormalized grouped records into a collection of normalized groupings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TGroupKey"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="source">An ordered set of data ordered by the group key</param>
        /// <param name="getGroupKey">Gets the group key from the current record used to differentiate between groups</param>
        /// <param name="groupKeyComparer">An optional function to compare group keys for equality</param>
        /// <param name="selectGroup">Gets an instance to pair a group with its data</param>
        /// <returns></returns>
        public static IEnumerable<TResult> SelectFromOrderedGroups<T, TGroupKey, TResult>(
            this IEnumerable<T> source,
            Func<T, TGroupKey> getGroupKey,
            Func<TGroupKey, TGroupKey, bool> groupKeyComparer,
            Func<TGroupKey, List<T>, TResult> selectGroup
        )
        {
            if (getGroupKey == null) throw new ArgumentNullException("getGroupKey");
            if (selectGroup == null) throw new ArgumentNullException("selectGroup");
            if (source == null) yield break;

            Maybe<TGroupKey> lastKey = Maybe<TGroupKey>.Nothing;
            List<T> range = new List<T>();

            var keyEq = groupKeyComparer;
            if (keyEq == null)
            {
                keyEq = (a, b) => EqualityComparer<TGroupKey>.Default.Equals(a, b);
            }

            using (var en = source.GetEnumerator())
                while (en.MoveNext())
                {
                    T curr = en.Current;

                    var currKey = getGroupKey(curr);
                    if (!lastKey.HasValue)
                    {
                        // First element has no previous element to compare to:
                    }
                    else if (!keyEq(lastKey.Value, currKey))
                    {
                        // Yield the last group produced:
                        yield return selectGroup(lastKey.Value, range);

                        // Start a new group:
                        range = new List<T>();
                    }

                    // This element goes to the current group:
                    range.Add(curr);
                    lastKey = currKey;
                }

            // Any elements for the last group?
            if (lastKey.HasValue) yield return selectGroup(lastKey.Value, range);
            yield break;
        }

        public static T[] Slice<T>(this T[] arr, int startIndex)
        {
            if (arr == null) return null;

            return Slice(arr, startIndex, arr.Length - startIndex);
        }

        public static T[] Slice<T>(this T[] arr, int startIndex, int length)
        {
            if (arr == null) return null;

            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");

            // 0-length slice returns an empty array:
            if (length == 0) return new T[0];

            if (startIndex >= arr.Length) throw new ArgumentOutOfRangeException("startIndex");

            // Negative length is relative to array's length:
            if (length < 0) length = (arr.Length - startIndex) + length;

            // Total length is out of range?
            if (length > arr.Length) throw new ArgumentOutOfRangeException("length");

            // Take the slice:
            T[] slice = new T[length];
            Array.Copy(arr, startIndex, slice, 0, length);
            return slice;
        }

        public static T[] ToArray<T>(this IEnumerable<T> src, int length)
        {
            T[] arr = new T[length];
            using (IEnumerator<T> e = src.GetEnumerator())
            {
                for (int i = 0; e.MoveNext(); ++i)
                {
                    arr[i] = e.Current;
                }
                return arr;
            }
        }

        public static T[] AppendAsArray<T>(this T[] src, T element)
        {
            T[] arr = new T[src.Length + 1];
            Array.Copy(src, arr, src.Length);
            arr[src.Length] = element;
            return arr;
        }

        public static T[] AppendAsArray<T>(this IEnumerable<T> src, T element, int length)
        {
            T[] arr = new T[length + 1];
            using (IEnumerator<T> e = src.GetEnumerator())
            {
                for (int i = 0; e.MoveNext(); ++i)
                {
                    arr[i] = e.Current;
                }
                arr[length] = element;
                return arr;
            }
        }

        public static List<T> ToList<T>(this IEnumerable<T> src, int initialCapacity)
        {
            List<T> lst = new List<T>(initialCapacity);
            using (IEnumerator<T> e = src.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    lst.Add(e.Current);
                }
                return lst;
            }
        }

        /// <summary>
        /// Uses ThreadPool.QueueUserWorkItem to schedule a background thread that enumerates the query and
        /// builds a List&lt;<typeparamref name="T"/>&gt; to hold the results.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">Query to enumerate on the background thread</param>
        /// <param name="completion">Action to handle the results of the query; executed on the background thread</param>
        /// <param name="initialCapacity">An expected count of results from the query; used for the initial capacity of List</param>
        /// <returns>A WaitHandle that will be signaled on completion of the query enumeration.</returns>
        /// <remarks>
        /// Enumerating the results of <paramref name="query"/> should be side-effect free and should be safe to execute on a separate thread.
        /// </remarks>
        public static System.Threading.WaitHandle BackgroundEnumerate<T>(this IEnumerable<T> query, Action<List<T>> completion, int initialCapacity)
        {
            System.Threading.ManualResetEvent ev = new System.Threading.ManualResetEvent(false);

            System.Threading.ThreadPool.QueueUserWorkItem(delegate(object o)
            {
                try
                {
                    List<T> results = new List<T>(initialCapacity);
                    results.AddRange(query);

                    completion(results);
                }
                finally
                {
                    ev.Set();
                }
            });

            return ev;
        }
    }
}
