using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WellDunne.Standards
{
    public struct ISO8601DateTime
    {
        private readonly DateTimeOffset _date;

        public ISO8601DateTime(DateTimeOffset date)
        {
            _date = date.ToUniversalTime();
        }

        public override string ToString()
        {
            return _date.ToString("yyyyMMddTHHmmssZ");
        }

        public static implicit operator DateTimeOffset(ISO8601DateTime date)
        {
            return date._date;
        }

        public static implicit operator ISO8601DateTime(DateTimeOffset date)
        {
            return new ISO8601DateTime(date);
        }

        public static implicit operator string(ISO8601DateTime date)
        {
            return date.ToString();
        }

        private static readonly string[] _formats = new string[] { "yyyyMMddTHHmmssK", "yyyyMMddTHHmmssZ", "yyyyMMddTHHmmss.ffK", "yyyyMMddTHHmmss.ffZ" };

        public static ISO8601DateTime Parse(string date)
        {
            DateTimeOffset tmp;
            if (!DateTimeOffset.TryParseExact(date, _formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out tmp))
            {
                throw new ArgumentException("Value is not in proper ISO8601 format", "date");
            }
            return new ISO8601DateTime(tmp);
        }

        public static explicit operator ISO8601DateTime(string date)
        {
            return Parse(date);
        }
    }
}
