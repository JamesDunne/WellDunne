using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WellDunne.Expressions
{
    public abstract class BinaryExpression : Expression
    {
        protected readonly Expression _l;
        protected readonly Expression _r;

        public Expression Left { get { return _l; } }
        public Expression Right { get { return _r; } }

        protected BinaryExpression(Expression l, Expression r)
        {
            _l = l;
            _r = r;
        }

        protected abstract void WriteInner(TextWriter tw);

        public override void WriteTo(TextWriter tw)
        {
            tw.Write("(");
            Left.WriteTo(tw);
            WriteInner(tw);
            Right.WriteTo(tw);
            tw.Write(")");
        }
    }
}
