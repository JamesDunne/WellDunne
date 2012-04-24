using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class StringParseExtensions
    {
        #region Int32 conversions

        public static Int32 ToInt32(this string strValue)
        {
            return Int32.Parse(strValue);
        }

        public static Int32 ToInt32Or(this string strValue, Int32 defaultValue)
        {
            Int32 value;
            if (Int32.TryParse(strValue, out value)) return value;
            return defaultValue;
        }

        public static Int32? ToInt32OrNull(this string strValue)
        {
            Int32 value;
            if (Int32.TryParse(strValue, out value)) return (Int32?)value;
            return (Int32?)null;
        }

        #endregion

        #region Boolean conversions

        public static Boolean ToBoolean(this string strValue)
        {
            return Boolean.Parse(strValue);
        }

        public static Boolean ToBooleanOr(this string strValue, Boolean defaultValue)
        {
            Boolean value;
            if (Boolean.TryParse(strValue, out value)) return value;
            return defaultValue;
        }

        public static Boolean? ToBooleanOrNull(this string strValue)
        {
            Boolean value;
            if (Boolean.TryParse(strValue, out value)) return (Boolean?)value;
            return (Boolean?)null;
        }

        #endregion

        #region Boolean conversions

        public static DateTime ToDateTime(this string strValue)
        {
            return DateTime.Parse(strValue);
        }

        public static DateTime ToDateTimeOr(this string strValue, DateTime defaultValue)
        {
            DateTime value;
            if (DateTime.TryParse(strValue, out value)) return value;
            return defaultValue;
        }

        public static DateTime? ToDateTimeOrNull(this string strValue)
        {
            DateTime value;
            if (DateTime.TryParse(strValue, out value)) return (DateTime?)value;
            return (DateTime?)null;
        }

        #endregion
    }
}
