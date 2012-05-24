using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WellDunne.Expressions
{
    public sealed class NullExpression : Expression
    {
        private readonly Token _token;

        public NullExpression(Token tok)
        {
            _token = tok;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write("null");
        }
    }
}
