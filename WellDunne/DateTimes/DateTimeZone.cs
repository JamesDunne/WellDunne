using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace System
{
    /// <summary>
    /// Represents a UTC DateTime paired with a TimeZoneInfo for proper DST and UTC offset handling.
    /// </summary>
    public struct DateTimeZone : IEquatable<DateTimeZone>, IComparable<DateTimeZone>
    {
        /// <summary>
        /// DateTime in UTC.
        /// </summary>
        private readonly DateTime _date;
        /// <summary>
        /// Time zone.
        /// </summary>
        private readonly TimeZoneInfo _zone;

        /// <summary>
        /// Privately construct a DateTimeZone from a UTC date and a TimeZoneInfo. Use ConvertToTimeZone to publicly construct one.
        /// </summary>
        /// <param name="date">Kind must be DateTimeKind.Utc</param>
        /// <param name="zone">Time zone (cannot be null)</param>
        private DateTimeZone(DateTime date, TimeZoneInfo zone)
        {
            Debug.Assert(zone != null);
            Debug.Assert(date.Kind == DateTimeKind.Utc);

            _date = date;
            _zone = zone;
        }

        /// <summary>
        /// Gets the hash code of this DateTimeZone.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() { return DateTimeOffset.GetHashCode(); }

        public override bool Equals(object obj) { return Equals((DateTimeZone)obj); }

        /// <summary>
        /// Compares the UTC DateTime value of this DateTimeZone for equality with the UTC DateTime value of the other DateTimeZone.
        /// </summary>
        /// <param name="other">DateTimeZone to compare to</param>
        /// <returns></returns>
        public bool Equals(DateTimeZone other) { return _date.Equals(other._date); }
        /// <summary>
        /// Compares the UTC DateTime value of this DateTimeZone with the UTC DateTime value of the other DateTimeZone.
        /// </summary>
        /// <param name="other">DateTimeZone to compare to</param>
        /// <returns></returns>
        public int CompareTo(DateTimeZone other) { return _date.CompareTo(other._date); }

        public static bool operator ==(DateTimeZone a, DateTimeZone b) { return a.Equals(b); }
        public static bool operator !=(DateTimeZone a, DateTimeZone b) { return !(a.Equals(b)); }
        public static implicit operator DateTimeOffset(DateTimeZone dz) { return dz.DateTimeOffset; }
        public static implicit operator DateTime(DateTimeZone dz) { return dz._date; }

        /// <summary>
        /// Creates a DateTimeZone to pair a DateTime in UTC with a TimeZoneInfo, adjusting the DateTime value to UTC as necessary.
        /// </summary>
        /// <param name="date">A DateTime value of any Kind to be converted to UTC.
        /// DateTimeKind.Local is assumed to be in the system's local time zone.
        /// DateTimeKind.Unspecified is assumed to be in the given time zone.</param>
        /// <param name="zone">The TimeZoneInfo that represents the time zone.</param>
        /// <returns></returns>
        public static DateTimeZone Create(DateTime date, TimeZoneInfo zone)
        {
            if (zone == null) throw new ArgumentNullException("zone");

            DateTime utc;

            // Unspecified kinds are treated as being local in the given timezone:
            if (date.Kind == DateTimeKind.Unspecified)
                utc = new DateTime(date.Subtract(zone.GetUtcOffset(date)).Ticks, DateTimeKind.Utc);
            else
                // Local and Utc kinds are converted to their proper DateTimeOffset values (system local or UTC offset) and then converted to the given timezone:
                utc = new DateTimeOffset(date).UtcDateTime;

            return new DateTimeZone(utc, zone);
        }

        /// <summary>
        /// Creates a DateTimeZone to pair a DateTime in UTC with a TimeZoneInfo, adjusting the DateTime value to UTC as necessary.
        /// </summary>
        /// <param name="date">A DateTime value of any Kind to be converted to UTC.
        /// DateTimeKind.Local is assumed to be in the system's local time zone.
        /// DateTimeKind.Unspecified is assumed to be in the given time zone.</param>
        /// <param name="timeZoneId">The Windows identifier of the time zone.</param>
        /// <returns></returns>
        public static DateTimeZone Create(DateTime date, string timeZoneId)
        {
            if (timeZoneId == null) throw new ArgumentNullException("timeZoneId");

            TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return Create(date, zone);
        }

        /// <summary>
        /// Creates a DateTimeZone to pair a DateTime in UTC with a TimeZoneInfo, adjusting the DateTime value to UTC as necessary.
        /// </summary>
        /// <param name="date">A DateTimeOffset value to be converted to UTC.</param>
        /// <param name="zone">The TimeZoneInfo that represents the time zone.</param>
        /// <returns></returns>
        public static DateTimeZone Create(DateTimeOffset date, TimeZoneInfo zone)
        {
            if (zone == null) throw new ArgumentNullException("zone");

            var utc = date.UtcDateTime;
            if (zone.IsInvalidTime(utc)) throw new ArgumentException("DateTimeOffset represents an invalid time in the time zone", "date");

            return new DateTimeZone(utc, zone);
        }

        /// <summary>
        /// Creates a DateTimeZone to pair a DateTime in UTC with a TimeZoneInfo, adjusting the DateTime value to UTC as necessary.
        /// </summary>
        /// <param name="date">A DateTimeOffset value to be converted to UTC.</param>
        /// <param name="timeZoneId">The Windows identifier of the time zone.</param>
        /// <returns></returns>
        public static DateTimeZone Create(DateTimeOffset date, string timeZoneId)
        {
            if (timeZoneId == null) throw new ArgumentNullException("timeZoneId");

            TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return Create(date, zone);
        }

        /// <summary>
        /// Gets a DateTimeOffset that represents this DateTimeZone.
        /// </summary>
        public DateTimeOffset DateTimeOffset
        {
            get
            {
                var zoned = ZonedDateTime;
                var dto = new DateTimeOffset(zoned, _zone.GetUtcOffset(_date));
                return dto;
            }
        }

        /// <summary>
        /// Gets the DateTime local to the time zone.
        /// </summary>
        public DateTime ZonedDateTime
        {
            get
            {
                var offs = _zone.GetUtcOffset(_date);
                var offsAdjusted = _date.Add(offs);
                var newDate = new DateTime(offsAdjusted.Ticks, DateTimeKind.Unspecified);
                return newDate;
            }
        }

        /// <summary>
        /// Gets the DateTime in UTC, regardless of time zone.
        /// </summary>
        public DateTime UtcDateTime { get { return _date; } }
        /// <summary>
        /// Gets the TimeZoneInfo.
        /// </summary>
        public TimeZoneInfo TimeZoneInfo { get { return _zone; } }
        /// <summary>
        /// Gets the UTC offset for the DateTime.
        /// </summary>
        public TimeSpan UtcOffset { get { return _zone.GetUtcOffset(_date); } }

        /// <summary>
        /// Gets the zoned hour in 12-hour format.
        /// </summary>
        public int Hour12
        {
            get
            {
                int hh = (ZonedDateTime.Hour % 12);
                return hh == 0 ? 12 : hh;
            }
        }

        /// <summary>
        /// Is the zoned hour in AM?
        /// </summary>
        public bool IsAM { get { return ZonedDateTime.Hour < 12; } }
        /// <summary>
        /// Is the zoned hour in PM?
        /// </summary>
        public bool IsPM { get { return ZonedDateTime.Hour >= 12; } }

        /// <summary>
        /// Gets the zoned DateTime's Kind.
        /// </summary>
        public DateTimeKind Kind { get { return ZonedDateTime.Kind; } }
        /// <summary>
        /// Gets the zoned Date.
        /// </summary>
        public DateTime Date { get { return ZonedDateTime.Date; } }
        /// <summary>
        /// Gets the zoned day of the month.
        /// </summary>
        public int Day { get { return ZonedDateTime.Day; } }
        /// <summary>
        /// Gets the zoned month.
        /// </summary>
        public int Month { get { return ZonedDateTime.Month; } }
        /// <summary>
        /// Gets the zoned year.
        /// </summary>
        public int Year { get { return ZonedDateTime.Year; } }

        /// <summary>
        /// Gets the zoned day of the week.
        /// </summary>
        public DayOfWeek DayOfWeek { get { return ZonedDateTime.DayOfWeek; } }
        /// <summary>
        /// Gets the zoned day of the year.
        /// </summary>
        public int DayOfYear { get { return ZonedDateTime.DayOfYear; } }

        /// <summary>
        /// Gets the zoned hour in 24-hour format.
        /// </summary>
        public int Hour { get { return ZonedDateTime.Hour; } }
        /// <summary>
        /// Gets the zoned minute.
        /// </summary>
        public int Minute { get { return ZonedDateTime.Minute; } }
        /// <summary>
        /// Gets the zoned second.
        /// </summary>
        public int Second { get { return ZonedDateTime.Second; } }
        /// <summary>
        /// Gets the zoned millisecond.
        /// </summary>
        public int Millisecond { get { return ZonedDateTime.Millisecond; } }
        /// <summary>
        /// Gets the zoned ticks.
        /// </summary>
        public long Ticks { get { return ZonedDateTime.Ticks; } }
        /// <summary>
        /// Gets the zoned time of day.
        /// </summary>
        public TimeSpan TimeOfDay { get { return ZonedDateTime.TimeOfDay; } }

        /// <summary>
        /// Gets the local time zone's date and time.
        /// </summary>
        public static DateTimeZone Now { get { return new DateTimeZone(DateTime.UtcNow, TimeZoneInfo.Local); } }

        /// <summary>
        /// Gets a three-capital-letter abbreviation for the time zone sensitive to DST, e.g. "EST" vs. "EDT".
        /// </summary>
        /// <remarks>
        /// Notably US-centric.
        /// </remarks>
        public string TimeZoneAbbreviation
        {
            get
            {
                // TODO(jsd): Support more timezone abbreviations than US.
                if (_zone.IsDaylightSavingTime(_date))
                    switch (_zone.DaylightName)
                    {
                        case "Pacific Daylight Time": return "PDT";
                        case "Mountain Daylight Time": return "MDT";
                        case "Central Daylight Time": return "CDT";
                        case "Eastern Daylight Time": return "EDT";
                        default: return _zone.DaylightName;
                    }
                else
                    switch (_zone.StandardName)
                    {
                        case "Pacific Standard Time": return "PST";
                        case "Mountain Standard Time": return "MST";
                        case "US Mountain Standard Time": return "MST";
                        case "Central Standard Time": return "CST";
                        case "Eastern Standard Time": return "EST";
                        default: return _zone.StandardName;
                    }
            }
        }

        /// <summary>
        /// Adjusts the current DateTime UTC value to the new DateTime UTC value, accounting for any DST changes as necessary.
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns></returns>
        private DateTimeZone adjustTo(DateTime newValue)
        {
            Debug.Assert(newValue.Kind == DateTimeKind.Utc);
            var oldOffset = _zone.GetUtcOffset(_date);
            var newOffset = _zone.GetUtcOffset(newValue);
            return new DateTimeZone(newValue.Subtract(newOffset.Subtract(oldOffset)), _zone);
        }

        /// <summary>
        /// Return a new DateTimeZone modified by <paramref name="modify"/> from this DateTimeZone, adjusted for DST changes.
        /// </summary>
        /// <param name="modify">Function to modify the current DateTime value in UTC and return a new UTC value.</param>
        /// <returns></returns>
        public DateTimeZone AdjustForDST(Func<DateTime, DateTime> modify)
        {
            Debug.Assert(_date.Kind == DateTimeKind.Utc);

            // Attempt to adjust the value:
            var modified = modify(_date);

            // Adjustment must retain the DateTime in UTC:
            if (modified.Kind != DateTimeKind.Utc) throw new ArgumentException("`modify` function must return a new value with Kind = DateTimeKind.Utc", "modify");

            // Return the DST-adjusted value:
            return adjustTo(modified);
        }

        public DateTimeZone Add(TimeSpan value) { return new DateTimeZone(_date.Add(value), _zone); }
        public DateTimeZone AddHours(double value) { return new DateTimeZone(_date.AddHours(value), _zone); }
        public DateTimeZone AddMinutes(double value) { return new DateTimeZone(_date.AddMinutes(value), _zone); }
        public DateTimeZone AddSeconds(double value) { return new DateTimeZone(_date.AddSeconds(value), _zone); }
        public DateTimeZone AddMilliseconds(double value) { return new DateTimeZone(_date.AddMilliseconds(value), _zone); }
        public DateTimeZone AddTicks(long value) { return new DateTimeZone(_date.AddTicks(value), _zone); }
        public DateTimeZone AddDays(double value) { return new DateTimeZone(_date.AddDays(value), _zone); }
        public DateTimeZone AddMonths(int value) { return new DateTimeZone(_date.AddMonths(value), _zone); }
        public DateTimeZone AddYears(int value) { return new DateTimeZone(_date.AddYears(value), _zone); }
        public DateTimeZone Subtract(TimeSpan value) { return new DateTimeZone(_date.Subtract(value), _zone); }
        public TimeSpan Subtract(DateTimeZone value) { return value.UtcDateTime.Subtract(_date); }

        public DateTimeZone AddAdjusted(TimeSpan value) { return adjustTo(_date.Add(value)); }
        public DateTimeZone AddAdjustedHours(double value) { return adjustTo(_date.AddHours(value)); }
        public DateTimeZone AddAdjustedMinutes(double value) { return adjustTo(_date.AddMinutes(value)); }
        public DateTimeZone AddAdjustedSeconds(double value) { return adjustTo(_date.AddSeconds(value)); }
        public DateTimeZone AddAdjustedMilliseconds(double value) { return adjustTo(_date.AddMilliseconds(value)); }
        public DateTimeZone AddAdjustedTicks(long value) { return adjustTo(_date.AddTicks(value)); }
        public DateTimeZone AddAdjustedDays(double value) { return adjustTo(_date.AddDays(value)); }
        public DateTimeZone AddAdjustedMonths(int value) { return adjustTo(_date.AddMonths(value)); }
        public DateTimeZone AddAdjustedYears(int value) { return adjustTo(_date.AddYears(value)); }
        public DateTimeZone SubtractAdjusted(TimeSpan value) { return adjustTo(_date.Subtract(value)); }
        public TimeSpan SubtractAdjusted(DateTimeZone value)
        {
            var oldOffset = _zone.GetUtcOffset(_date);
            var newOffset = _zone.GetUtcOffset(value._date);
            return _date.Subtract(value._date).Subtract(newOffset.Subtract(oldOffset));
        }

        /// <summary>
        /// Sets the zoned time of day.
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public DateTimeZone SetTimeOfDay(TimeSpan timeOfDay) { return new DateTimeZone(_date.Subtract(TimeOfDay).Add(timeOfDay), _zone); }

        /// <summary>
        /// Gets this week's Sunday at 12AM, adjusting for DST as necessary.
        /// </summary>
        /// <returns></returns>
        public DateTimeZone ToWeekStart()
        {
            return Subtract(TimeOfDay).AddAdjustedDays(-(int)_date.DayOfWeek);
        }

        /// <summary>
        /// Gets next week's Sunday at 12AM, adjusting for DST as necessary.
        /// </summary>
        /// <returns></returns>
        public DateTimeZone ToWeekEndExclusive()
        {
            return Subtract(TimeOfDay).AddAdjustedDays(7 - (int)_date.DayOfWeek);
        }

        /// <summary>
        /// Gets this week's Saturday at the latest possible tick of the day, adjusting for DST as necessary.
        /// </summary>
        /// <returns></returns>
        public DateTimeZone ToWeekEndInclusive()
        {
            return Subtract(TimeOfDay).AddAdjustedDays(7 - (int)_date.DayOfWeek).Subtract(TimeSpan.FromTicks(1L));
        }
    }
}
