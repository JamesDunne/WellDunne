using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Text
{
    public static class UTF8
    {
        /// <summary>
        /// Specific version of UTF-8 encoding to bypass generating the Byte-Order-Marker.
        /// </summary>
        public static readonly System.Text.UTF8Encoding EncodingNoBOM = new System.Text.UTF8Encoding(false);
    }
}
