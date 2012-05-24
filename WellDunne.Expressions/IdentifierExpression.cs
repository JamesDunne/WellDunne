using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WellDunne.Expressions
{
    public class IdentifierExpression : Expression
    {
        private readonly Token _token;
        public IdentifierExpression(Token token)
        {
            _token = token;
        }
    }
}
