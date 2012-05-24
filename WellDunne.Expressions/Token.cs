using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WellDunne.Expressions
{
    [System.Diagnostics.DebuggerDisplay("{Kind} - {Value}")]
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

        public override string ToString()
        {
            switch (_kind)
            {
                case TokenKind.Invalid: return "<INVALID>";
                case TokenKind.Identifier: return _value;
                case TokenKind.Operator: return _value;
                case TokenKind.IntegerLiteral: return _value;
                case TokenKind.DecimalLiteral: return _value;
                case TokenKind.Null: return "null";
                case TokenKind.True: return "true";
                case TokenKind.False: return "false";
                case TokenKind.ParenOpen: return "(";
                case TokenKind.ParenClose: return ")";
                case TokenKind.BracketOpen: return "[";
                case TokenKind.BracketClose: return "]";
                case TokenKind.Comma: return ",";
                case TokenKind.StringLiteral: return String.Concat("\'", escapeString(_value), "\'");
                default: return String.Format("<unknown token {0}>", _value ?? _kind.ToString());
            }
        }

        internal static string kindToString(TokenKind kind)
        {
            switch (kind)
            {
                case TokenKind.Invalid: return "<INVALID>";
                case TokenKind.Identifier: return "identifier";
                case TokenKind.Operator: return "operator";
                case TokenKind.IntegerLiteral: return "integer";
                case TokenKind.DecimalLiteral: return "decimal";
                case TokenKind.Null: return "null";
                case TokenKind.True: return "true";
                case TokenKind.False: return "false";
                case TokenKind.ParenOpen: return "(";
                case TokenKind.ParenClose: return ")";
                case TokenKind.BracketOpen: return "[";
                case TokenKind.BracketClose: return "]";
                case TokenKind.Comma: return ",";
                case TokenKind.StringLiteral: return "string";
                default: return String.Format("<unknown token kind {0}>", kind.ToString());
            }
        }

        internal static string escapeString(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (char ch in value)
            {
                if (ch == '\n') sb.Append("\\n");
                else if (ch == '\r') sb.Append("\\r");
                else if (ch == '\t') sb.Append("\\t");
                else if (ch == '\\') sb.Append("\\\\");
                else if (ch == '\'') sb.Append("\\\'");
                else if (ch == '\"') sb.Append("\\\"");
                else sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}
