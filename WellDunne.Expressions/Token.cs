using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WellDunne.Expressions
{
    public struct Token
    {
        private readonly TokenKind _kind;
        private readonly long _position;
        private readonly string _value;

        public TokenKind Kind { get { return _kind; } }
        public long Position { get { return _position; } }
        public string Value { get { return _value; } }

        public Token(TokenKind kind, long position, string value)
        {
            _kind = kind;
            _position = position;
            _value = value;
        }

        public Token(TokenKind kind, long position)
        {
            _kind = kind;
            _position = position;
            _value = null;
        }
    }
}
