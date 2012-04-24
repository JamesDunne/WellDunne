using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace WellDunne.Debugging
{
    /// <summary>
    /// A TextWriter implementation that calls to Debug.Write for all writes. This class is only available in DEBUG builds.
    /// </summary>
    public sealed class DebugTextWriter : TextWriter
    {
        public string Category { get; private set; }

        public DebugTextWriter(string category)
        {
            this.Category = category;
        }

        public DebugTextWriter()
            : this(String.Empty)
        {
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }

        public override void Write(string value)
        {
            Debug.Write(value, Category);
        }

        public override void WriteLine(string value)
        {
            Debug.WriteLine(value, Category);
        }

        public override void Write(char ch)
        {
            Debug.Write(ch.ToString(), Category);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            Debug.Write(new string(buffer, index, count), Category);
        }
    }
}
