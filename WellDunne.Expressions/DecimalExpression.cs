using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WellDunne.Expressions
{
    public sealed class DecimalExpression : Expression
    {
        private readonly Token _token;

        public DecimalExpression(Token tok)
        {
            this._token = tok;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write(_token.Value);
        }
    }
}
