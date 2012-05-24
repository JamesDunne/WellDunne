using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WellDunne.Expressions
{
    public abstract class Expression
    {
        /// <summary>
        /// Writes the expression to the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="tw"></param>
        public virtual void WriteTo(TextWriter tw)
        {
            tw.Write("<expr>");
        }

        /// <summary>
        /// Formats the expression as a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            using (var tw = new StringWriter())
            {
                WriteTo(tw);
                return tw.ToString();
            }
        }
    }
}
