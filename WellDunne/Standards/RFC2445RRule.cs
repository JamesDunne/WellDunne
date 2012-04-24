using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace WellDunne.Standards
{
    public static class RFC2445RRule
    {
        struct RRuleParts
        {
            public readonly string Name;
            public readonly string[] Params;
            public readonly string[] Values;

            public RRuleParts(string name, string[] parms, string[] values)
            {
                Name = name;
                Params = parms;
                Values = values;
            }
        }

        private static readonly char[] paramSplitChars = new char[1] { ';' };
        private static readonly char[] valueSplitChars = new char[1] { ',' };

        static RRuleParts Parse(string rrule)
        {
            if (rrule == null) throw new ArgumentNullException("rrule");

            int cln = rrule.IndexOf(':');
            if (cln == -1) throw new ArgumentException("Required ':' not found", "rrule");

            string paramsStr = rrule.Substring(0, cln);
            string valuesStr = rrule.Substring(cln + 1);

            string[] parms = paramsStr.Split(paramSplitChars, StringSplitOptions.RemoveEmptyEntries);
            string[] values = valuesStr.Split(valueSplitChars, StringSplitOptions.RemoveEmptyEntries);

            return new RRuleParts(parms[0], parms.Slice(1), values);
        }

        struct Period
        {
            public DateTimeOffset Date { get; private set; }
            public TimeSpan Span { get; private set; }

            public Period(DateTimeOffset date, TimeSpan span)
                : this()
            {
                Date = date;
                Span = span;
            }
        }

        static List<Period> CalculatePeriods(IList<string> rrules, TimeSpan defaultPeriod)
        {
            var pers = new List<Period>(rrules.Count);

            foreach (var rrule in rrules)
            {
                var parts = RFC2445RRule.Parse(rrule);
                switch (parts.Name.ToUpper())
                {
                    case "RDATE":
                        if (parts.Params.Any(p => p.StartsWith("VALUE=", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (parts.Params.SingleOrDefault(p => p == "VALUE=DATE-TIME") != null)
                            {
                                foreach (var value in parts.Values)
                                    pers.Add(new Period((ISO8601DateTime)value, defaultPeriod));
                            }
                            else if (parts.Params.SingleOrDefault(p => p == "VALUE=DATE") != null)
                            {
                                foreach (var value in parts.Values)
                                    pers.Add(new Period((ISO8601DateTime)value, defaultPeriod));
                            }
                            else if (parts.Params.SingleOrDefault(p => p == "VALUE=PERIOD") != null)
                            {
                                foreach (var value in parts.Values)
                                {
                                    string[] valparts = value.Split('/');
                                    pers.Add(new Period((ISO8601DateTime)valparts[0], (ISO8601TimeSpan)valparts[1]));
                                }
                            }
                            else
                            {
                                // Invalid.
                            }
                        }
                        else
                        {
                            // Try to detect each case:
                            foreach (var value in parts.Values)
                            {
                                if (value.Contains('/'))
                                {
                                    string[] valparts = value.Split('/');
                                    pers.Add(new Period((ISO8601DateTime)valparts[0], (ISO8601TimeSpan)valparts[1]));
                                }
                                else
                                {
                                    pers.Add(new Period((ISO8601DateTime)value, defaultPeriod));
                                }
                            }
                        }
                        break;
                    default: break;
                }
            }

            return pers;
        }

        public static bool RRulesAreEqual(IList<string> arules, TimeSpan aper, IList<string> brules, TimeSpan bper)
        {
            // Calculate the sets of recurring periods and sort by date:
            var apers = CalculatePeriods(arules, aper).OrderBy(p => p.Date).ThenBy(p => p.Span).ToList();
            var bpers = CalculatePeriods(brules, bper).OrderBy(p => p.Date).ThenBy(p => p.Span).ToList();

            if (apers.Count != bpers.Count) return false;
            Debug.Assert(apers.Count == bpers.Count);

            // Compare each period:
            for (int i = 0; (i < apers.Count); ++i)
            {
                if (apers[i].Date != bpers[i].Date) return false;
                if (apers[i].Span != bpers[i].Span) return false;
            }

            return true;
        }
    }
}
