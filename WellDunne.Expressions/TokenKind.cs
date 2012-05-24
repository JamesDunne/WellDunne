using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WellDunne.Expressions
{
    public enum TokenKind
    {
        Invalid,

        Identifier,
        True,
        False,
        Null,
        Operator,

        StringLiteral,
        IntegerLiteral,
        DecimalLiteral,

        Comma,
        ParenOpen,
        ParenClose,
        BracketOpen,
        BracketClose
    }
}
