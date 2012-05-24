using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WellDunne.Expressions
{
    public sealed class BooleanExpression : Expression
    {
        private readonly Token _token;
        private readonly bool _value;

        public BooleanExpression(Token tok, bool value)
        {
            this._token = tok;
            this._value = value;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write(_value ? "true" : "false");
        }
    }
}
