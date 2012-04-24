using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text
{
    public static class StringBuilderFormatExtensions
    {
        public static void AppendLine(this StringBuilder sb, string format, params object[] args)
        {
            sb.AppendLine(String.Format(format, args));
        }

        public static void Append(this StringBuilder sb, string format, params object[] args)
        {
            sb.Append(String.Format(format, args));
        }
    }
}