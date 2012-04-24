using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Web
{
    public static class HtmlExtensions
    {
        /// <summary>
        /// HTML-encode the string and convert newlines into &lt;br/&gt; tags.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string HtmlEncodeNewlinesToBRTags(this string input)
        {
            return HttpUtility.HtmlEncode(input).Replace("\r\n", "\n").Replace("\n", "<br/>\r\n");
        }
    }
}
