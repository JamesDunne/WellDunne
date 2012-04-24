using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace WellDunne.Standards
{
    public struct ISO8601TimeSpan
    {
        private readonly TimeSpan _span;

        public ISO8601TimeSpan(TimeSpan span)
        {
            _span = span.Duration();
        }

        public override string ToString()
        {
            var sbDate = new StringBuilder();
            var sbTime = new StringBuilder();

            // TODO(jsd): Specify years, months, days
            if (_span.Days > 0) sbDate.Append("{0}D", _span.Days);
            
            // Time components:
            if (_span.Hours > 0) sbTime.Append("{0}H", _span.Hours);
            if (_span.Minutes > 0) sbTime.Append("{0}M", _span.Minutes);
            if (_span.Seconds > 0) sbTime.Append("{0}S", _span.Seconds);

            return String.Concat("P", sbDate.ToString(), "T", sbTime.ToString());
        }

        public static implicit operator TimeSpan(ISO8601TimeSpan span)
        {
            return span._span;
        }

        public static implicit operator ISO8601TimeSpan(TimeSpan span)
        {
            return new ISO8601TimeSpan(span);
        }

        public static implicit operator string(ISO8601TimeSpan span)
        {
            return span.ToString();
        }

        public static ISO8601TimeSpan Parse(string span)
        {
            // NOTE(jsd): This is incomplete because it does not accommodate for fractional measurements and it does not accommodate weeks, months, or years.
            if (span == null) throw new ArgumentNullException("span");
            if (span.Length < 4) throw new ArgumentOutOfRangeException("span");

            if (span[0] != 'P') throw new ArgumentOutOfRangeException("span");

            int start;
            int c = 1;

            int days = 0;
            int hours = 0;
            int mins = 0;
            int secs = 0;
            bool postT = false;

            while (c < span.Length)
            {
                // The 'T' separator separates (year, month, week, day) from (hour, minute, second) and is used
                // primarily to disambiguate the 'M' suffix meaning both MONTH and MINUTES.
                if (span[c] == 'T')
                {
                    postT = true;
                    ++c;
                    continue;
                }

                // Read a series of digits:
                start = c;
                while (c < span.Length)
                {
                    // TODO(jsd): Allow decimal points.
                    if (!Char.IsDigit(span[c]))
                        break;

                    ++c;
                }

                if (c >= span.Length) break;

                // TODO(jsd): Switch `Int32.Parse()` to `Double.Parse()` and use `TimeSpan.FromXYZ(double value)`.
                if (!postT)
                {
                    // Years, Months, Weeks, Days:
                    switch (span[c])
                    {
                        // Days:
                        case 'D':
                            days = Int32.Parse(span.Substring(start, c - start));
                            break;
                        // TODO(jsd): support Years, Months, Weeks, etc.
                        default:
                            break;
                    }
                }
                else
                {
                    // Hours, Minutes, Seconds:
                    switch (span[c])
                    {
                        case 'H':
                            hours = Int32.Parse(span.Substring(start, c - start));
                            break;
                        case 'M':
                            mins = Int32.Parse(span.Substring(start, c - start));
                            break;
                        case 'S':
                            secs = Int32.Parse(span.Substring(start, c - start));
                            break;
                    }
                }

                ++c;
            }

            return new ISO8601TimeSpan(new TimeSpan(days, hours, mins, secs));
        }

        public static explicit operator ISO8601TimeSpan(string span)
        {
            return Parse(span);
        }
    }
}
