using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WellDunne.Expressions
{
    public abstract class Expression
    {
        public virtual void WriteTo(TextWriter tw)
        {
            tw.Write("<expr>");
        }

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
