using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WellDunne.Expressions
{
    public sealed class EqualExpression : BinaryExpression
    {
        private readonly Token _token;

        public EqualExpression(Token tok, Expression l, Expression r)
            : base(l, r)
        {
            _token = tok;
        }

        protected override void WriteInner(TextWriter tw)
        {
            tw.Write(" " + _token.Value + " ");
        }
    }
}
