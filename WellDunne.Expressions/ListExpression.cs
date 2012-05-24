using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WellDunne.Expressions
{
    public sealed class ListExpression : Expression
    {
        private readonly Token _token;
        private readonly List<Expression> _elements;

        public ListExpression(Token tok, List<Expression> elements)
        {
            _token = tok;
            _elements = elements;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write("[");
            for (int i = 0; i < _elements.Count; ++i)
            {
                _elements[i].WriteTo(tw);
                if (i < _elements.Count - 1) tw.Write(",");
            }
            tw.Write("]");
        }
    }
}
