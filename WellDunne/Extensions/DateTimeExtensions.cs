using System;

namespace System
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Formats this date/time/offset per RFC3339 standard (http://www.ietf.org/rfc/rfc3339.txt).
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public static string ToRFC3339(this DateTimeOffset dto)
        {
            return dto.ToString("yyyy-MM-ddTHH:mm:ssK");
        }

        /// <summary>
        /// Formats this date/time/offset per RFC3339 standard (http://www.ietf.org/rfc/rfc3339.txt) including optional fractional seconds.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public static string ToRFC3339withFractions(this DateTimeOffset dto)
        {
            return dto.ToString("yyyy-MM-ddTHH:mm:ss.ffK");
        }

        public static DateTimeOffset AdjustForTimeZoneAndDST(this DateTime dt, TimeZoneInfo timeZoneInfo)
        {
            if (timeZoneInfo == null) throw new ArgumentNullException("timeZoneInfo");
            return DateTimeZone.Create(dt, timeZoneInfo).DateTimeOffset;
        }

        public static DateTimeOffset AdjustForTimeZoneAndDST(this DateTime dt, string timeZoneId)
        {
            return DateTimeZone.Create(dt, timeZoneId).DateTimeOffset;
        }

        public static bool IsBetween(this DateTime value, DateTime start, DateTime end)
        {
            return (value <= start) && (value < end);
        }

        public static bool IsBetween(this DateTime value, DateTime? start, DateTime? end)
        {
            if (!start.HasValue && !end.HasValue) return false;
            else if (start.HasValue && !end.HasValue) return start.Value <= value;
            else if (!start.HasValue && end.HasValue) return value < end.Value;
            else return (value <= start.Value) && (value < end.Value);
        }

        public static DateTimeOffset SetTimeOfDay(this DateTimeOffset value, TimeSpan timeOfDay)
        {
            return value.Subtract(value.TimeOfDay).Add(timeOfDay);
        }

        public static DateTime SetTimeOfDay(this DateTime value, TimeSpan timeOfDay)
        {
            return value.Subtract(value.TimeOfDay).Add(timeOfDay);
        }

        public static int Hour12(this DateTimeOffset value)
        {
            int hh = (value.Hour % 12);
            return hh == 0 ? 12 : hh;
        }

        public static bool IsAM(this DateTimeOffset value)
        {
            return value.Hour < 12;
        }

        public static bool IsPM(this DateTimeOffset value)
        {
            return value.Hour >= 12;
        }

        public static string TimeZoneAbbreviation(this DateTimeOffset dt, TimeZoneInfo tz)
        {
            return DateTimeZone.Create(dt, tz).TimeZoneAbbreviation;
        }

        public static string TimeZoneAbbreviation(this DateTime dt, TimeZoneInfo tz)
        {
            return DateTimeZone.Create(dt, tz).TimeZoneAbbreviation;
        }

        public static string ToUtcOffsetString(this TimeSpan value)
        {
            return String.Format("{0:00}:{1:00}", value.Hours, value.Minutes);
        }
    }
}
