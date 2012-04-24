using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Collections.Generic
{
    public static class CollectionComparer
    {
        public static bool SortedEquals<T>(IList<T> alist, IList<T> blist, Comparison<T> sorter, [Optional] IEqualityComparer<T> equals)
        {
            // If both are null, they're equal:
            if ((alist == null) && (blist == null)) return true;
            // If comparing against null, they're not equal:
            if ((alist == null) != (blist == null)) return false;

            // If counts are not equal, they're not equal:
            if (alist.Count != blist.Count) return false;

            // Sort both lists:
            var asorted = new List<T>(alist);
            asorted.Sort(sorter);
            var bsorted = new List<T>(blist);
            bsorted.Sort(sorter);

            // Compare them:
            return Equals(asorted, bsorted, equals);
        }

        public static bool Equals<T>(IList<T> alist, IList<T> blist, [Optional] IEqualityComparer<T> equals)
        {
            // If both are null, they're equal:
            if ((alist == null) && (blist == null)) return true;
            // If comparing against null, they're not equal:
            if ((alist == null) != (blist == null)) return false;

            // If counts are not equal, they're not equal:
            if (alist.Count != blist.Count) return false;

            Debug.Assert(alist.Count == blist.Count);

            if (equals == null) equals = EqualityComparer<T>.Default;

            for (int i = 0; i < alist.Count; ++i)
            {
                // Early out and return false if an element does not match:
                if (!equals.Equals(alist[i], blist[i])) return false;
            }

            return true;
        }
    }
}
