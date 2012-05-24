using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WellDunne.Expressions
{
    public class LiteralExpression : Expression
    {
        private readonly Token _token;
        public LiteralExpression(Token token)
        {
            _token = token;
        }
    }
}
