using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WellDunne.Expressions
{
    public class OrExpression : BinaryExpression
    {
        private readonly Token _token;

        public OrExpression(Token tok, Expression l, Expression r)
            : base(l, r)
        {
            _token = tok;
        }

        protected override void WriteInner(TextWriter tw)
        {
            tw.Write(" or ");
        }
    }
}
