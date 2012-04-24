using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace System.Collections.Specialized
{
    public static class NameValueCollectionExtensions
    {
        public static string GetValueOrDefault(this NameValueCollection nvCollection, String key, String defaultValue)
        {
            String returnValue = defaultValue;
            if (nvCollection.AllKeys.Contains(key))
                returnValue = nvCollection[key];

            return returnValue;
        }
    }
}
