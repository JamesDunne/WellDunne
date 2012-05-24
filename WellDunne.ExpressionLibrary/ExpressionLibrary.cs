using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WellDunne.ExpressionLibrary
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

    [System.Diagnostics.DebuggerDisplay("{Kind} - {ToString()}")]
    public struct Token
    {
        private readonly TokenKind _kind;
        private readonly long _position;
        private readonly string _value;
        private readonly bool _isReservedWord;

        private static readonly HashSet<string> _reservedWords = new HashSet<string>(new string[] {
            "null", "true", "false",
            "eq", "ne", "lt", "gt", "le", "ge", "like", "in", "not", "and", "or"
        });

        public TokenKind Kind { get { return _kind; } }
        public long Position { get { return _position; } }
        public string Value { get { return _value; } }
        public bool IsReservedWord { get { return _isReservedWord; } }

        public Token(TokenKind kind, long position, string value)
        {
            _kind = kind;
            _position = position;
            _value = value;
            _isReservedWord = (kind == TokenKind.Identifier && _reservedWords.Contains(value));
        }

        public Token(TokenKind kind, long position)
        {
            _kind = kind;
            _position = position;
            _value = null;
            _isReservedWord = false;
        }

        public override string ToString()
        {
            switch (_kind)
            {
                case TokenKind.Invalid: return "<INVALID>";
                case TokenKind.Identifier: return identifier();
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

        private string identifier()
        {
            if (_isReservedWord) return "@" + _value;
            return _value;
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

    public sealed class Lexer
    {
        private readonly TextReader _reader;
        private long _charPosition;

        public Lexer(TextReader sr)
        {
            _reader = sr;
            _charPosition = 0;
        }

        public Lexer(TextReader sr, long charPosition)
        {
            _reader = sr;
            _charPosition = charPosition;
        }

        public IEnumerable<Token> Lex()
        {
            while (!EndOfStream())
            {
                // Peek at the current character:
                char c = Peek();
                if (Char.IsWhiteSpace(c))
                {
                    c = Read();
                    while (!EndOfStream() && Char.IsWhiteSpace(c = Peek()))
                    {
                        // Consume the whitespace:
                        Read();
                    }
                }

                // Record our current stream position:
                long position = Position();

                if (c == '_' || c == '@' || Char.IsLetter(c))
                {
                    // Start consuming an identifer:
                    var sb = new StringBuilder(8);

                    // Starting an identifier with '@' allows identifiers to be reserved words.
                    bool forceIdent = (c == '@');
                    if (c == '@') Read();

                    sb.Append(Read());
                    while (!EndOfStream())
                    {
                        char c2 = Peek();
                        if (c2 == '_' || Char.IsLetterOrDigit(c2))
                            sb.Append(Read());
                        else
                            break;
                    }

                    // Now determine what kind of token this identifier is:
                    string ident = sb.ToString();
                    if (forceIdent)
                        yield return new Token(TokenKind.Identifier, position, ident);
                    else if (ident == "null")
                        yield return new Token(TokenKind.Null, position, ident);
                    else if (ident == "true")
                        yield return new Token(TokenKind.True, position, ident);
                    else if (ident == "false")
                        yield return new Token(TokenKind.False, position, ident);
                    else if (operatorNames.Contains(ident))
                        yield return new Token(TokenKind.Operator, position, ident);
                    else
                        yield return new Token(TokenKind.Identifier, position, ident);
                }
                else if (Char.IsDigit(c) || c == '-')
                {
                    // Start consuming a numeric literal:
                    var sb = new StringBuilder(8);

                    bool isDecimal = false;
                    sb.Append(Read());

                    while (!EndOfStream())
                    {
                        char c2 = Peek();
                        if (isDecimal)
                        {
                            if (Char.IsDigit(c2))
                                sb.Append(Read());
                            else
                                break;
                        }
                        else
                        {
                            // HACK(jsd): Should create a new state to parse decimals properly.
                            if (c2 == '.')
                            {
                                isDecimal = true;
                                sb.Append(Read());
                            }
                            else if (Char.IsDigit(c2))
                                sb.Append(Read());
                            else
                                break;
                        }
                    }

                    if (isDecimal)
                        yield return new Token(TokenKind.DecimalLiteral, position, sb.ToString());
                    else
                        yield return new Token(TokenKind.IntegerLiteral, position, sb.ToString());
                }
                else if (c == '\'')
                {
                    bool error = false;

                    // Start consuming a quoted string:
                    var sb = new StringBuilder(32);
                    // Consume the opening quote char:
                    Read();

                    while (!error & !EndOfStream())
                    {
                        char c2 = Peek();
                        if (c2 == '\'')
                        {
                            // End of the string:
                            // Consume the '\'':
                            Read();
                            break;
                        }
                        else if (c2 == '\\')
                        {
                            // Escaped char:
                            // Consume the '\\':
                            Read();
                            // Consume the second char:
                            char c3;
                            switch (c3 = Read())
                            {
                                case '\\': sb.Append('\\'); break;
                                case '\'': sb.Append('\''); break;
                                case '\"': sb.Append('\"'); break;
                                case 'n': sb.Append('\n'); break;
                                case 'r': sb.Append('\r'); break;
                                case 't': sb.Append('\t'); break;
                                default:
                                    error = true;
                                    yield return new Token(TokenKind.Invalid, Position(), String.Format("Unrecognized escape sequence '\\{0}' at position {1}", c3, Position() + 1));
                                    break;
                            }
                        }
                        else
                        {
                            // Consume the char and add it to the string:
                            sb.Append(Read());
                        }
                    }

                    if (!error)
                        yield return new Token(TokenKind.StringLiteral, position, sb.ToString());
                }
                else if (c == ',')
                {
                    Read();
                    yield return new Token(TokenKind.Comma, position, c.ToString());
                }
                else if (c == '(')
                {
                    Read();
                    yield return new Token(TokenKind.ParenOpen, position, c.ToString());
                }
                else if (c == ')')
                {
                    Read();
                    yield return new Token(TokenKind.ParenClose, position, c.ToString());
                }
                else if (c == '[')
                {
                    Read();
                    yield return new Token(TokenKind.BracketOpen, position, c.ToString());
                }
                else if (c == ']')
                {
                    Read();
                    yield return new Token(TokenKind.BracketClose, position, c.ToString());
                }
                else
                {
                    Read();
                    yield return new Token(TokenKind.Invalid, position, String.Format("Unrecognized character '{0}' at position {1}", c, position + 1));
                }
            }

            yield break;
        }

        private char Read()
        {
            ++_charPosition;
            return (char)_reader.Read();
        }

        private char Peek() { return (char)_reader.Peek(); }
        private long Position() { return _charPosition; }
        private bool EndOfStream() { return _reader.Peek() == -1; }

        private static readonly HashSet<string> operatorNames = new HashSet<string>(new string[] {
            "eq", "ne", "lt", "gt", "le", "ge", "like", "in", "not", "and", "or"
        });
    }

    public abstract class Expression
    {
        /// <summary>
        /// Writes the expression to the given <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="tw"></param>
        public virtual void WriteTo(TextWriter tw)
        {
            tw.Write("<expr>");
        }

        /// <summary>
        /// Formats the expression as a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            using (var tw = new StringWriter())
            {
                WriteTo(tw);
                return tw.ToString();
            }
        }
    }

    public class IdentifierExpression : Expression
    {
        private readonly Token _token;

        public IdentifierExpression(Token token)
        {
            _token = token;
        }

        public override void WriteTo(TextWriter tw)
        {
            if (_token.IsReservedWord) tw.Write('@');
            tw.Write(_token.Value);
        }
    }

    public sealed class StringExpression : Expression
    {
        private readonly Token _token;

        public StringExpression(Token tok)
        {
            this._token = tok;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write(_token);
        }
    }

    public sealed class IntegerExpression : Expression
    {
        private readonly Token _token;

        public IntegerExpression(Token tok)
        {
            this._token = tok;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write(_token.Value);
        }
    }

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

    public sealed class NullExpression : Expression
    {
        private readonly Token _token;

        public NullExpression(Token tok)
        {
            _token = tok;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write("null");
        }
    }

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

    public sealed class CompareExpression : BinaryExpression
    {
        private readonly Token _token;

        public CompareExpression(Token tok, Expression l, Expression r)
            : base(l, r)
        {
            _token = tok;
        }

        protected override void WriteInner(TextWriter tw)
        {
            tw.Write(" " + _token.Value + " ");
        }
    }

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

    public sealed class AndExpression : BinaryExpression
    {
        private readonly Token _token;

        public AndExpression(Token tok, Expression l, Expression r)
            : base(l, r)
        {
            _token = tok;
        }

        protected override void WriteInner(TextWriter tw)
        {
            tw.Write(" and ");
        }
    }

    public sealed class EqualExpression : BinaryExpression
    {
        private readonly Token _token;

        public EqualExpression(Token tok, Expression l, Expression r)
            : base(l, r)
        {
            _token = tok;
        }

        protected override void WriteInner(TextWriter tw)
        {
            tw.Write(" " + _token.Value + " ");
        }
    }

    public sealed class ParserError
    {
        private readonly Token _token;
        private readonly string _message;

        public Token Token { get { return _token; } }
        public string Message { get { return _message; } }

        public ParserError(Token tok, string message)
        {
            _token = tok;
            _message = message;
        }
    }

    /// <summary>
    /// Main entry point to the expression parser.
    /// </summary>
    public sealed class Parser
    {
        private readonly Lexer _lexer;
        private IEnumerator<Token> _tokens;
        private bool _eof;
        private Token _lastToken;
        private List<ParserError> _errors;
        private int _position;

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            _tokens = _lexer.Lex().GetEnumerator();
            _errors = new List<ParserError>();
        }

        /// <summary>
        /// Parses the expression lexed by the lexer and returns a boolean that indicates success or failure.
        /// </summary>
        /// <param name="result">Resulting expression instance that represents the parsed expression tree or null if failed</param>
        /// <returns>true if succeeded in parsing, false otherwise</returns>
        public bool ParseExpression(out Expression result)
        {
            using (_tokens)
            {
                result = null;
                if (!AdvanceOrError("Expected expression")) return false;

                bool success = parseExpression(out result);
                if (!success) return false;

                if (!Eof())
                {
                    Error("Expected end of expression");
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Gets the collection of parser errors generated during the last ParseExpression.
        /// </summary>
        /// <returns></returns>
        public List<ParserError> GetErrors()
        {
            return new List<ParserError>(_errors);
        }

        private bool parseExpression(out Expression e)
        {
            if (!parseOrExp(out e)) return false;
            return true;
        }

        private bool parseOrExp(out Expression e)
        {
            if (!parseAndExp(out e)) return false;

            Expression e2;
            Token tok;

            while (Current.Kind == TokenKind.Operator && Current.Value == "or")
            {
                tok = Current;
                if (!AdvanceOrError("Expected expression after 'or'")) return false;
                if (!parseAndExp(out e2)) return false;
                e = new OrExpression(tok, e, e2);
            }
            return true;
        }

        private bool parseAndExp(out Expression e)
        {
            if (!parseCmpExp(out e)) return false;

            Expression e2;
            Token tok;

            while (Current.Kind == TokenKind.Operator && Current.Value == "and")
            {
                tok = Current;
                if (!AdvanceOrError("Expected expression after 'and'")) return false;
                if (!parseCmpExp(out e2)) return false;
                e = new AndExpression(tok, e, e2);
            }
            return true;
        }

        private bool parseCmpExp(out Expression e)
        {
            if (!parseUnaryExp(out e)) return false;
            if (Current.Kind != TokenKind.Operator) return true;

            Expression e2;
            Token tok = Current;

            if (tok.Value == "eq" ||
                tok.Value == "ne")
            {
                if (!AdvanceOrError(String.Format("Expected expression after '{0}'", tok.Value))) return false;
                if (!parseUnaryExp(out e2)) return false;
                e = new EqualExpression(tok, e, e2);
            }
            else if
               (tok.Value == "lt" ||
                tok.Value == "le" ||
                tok.Value == "gt" ||
                tok.Value == "ge" ||
                tok.Value == "like" ||
                tok.Value == "in")
            {
                if (!AdvanceOrError(String.Format("Expected expression after '{0}'", tok.Value))) return false;
                if (!parseUnaryExp(out e2)) return false;
                e = new CompareExpression(tok, e, e2);
            }

            return true;
        }

        private bool parseUnaryExp(out Expression e)
        {
            return parsePrimaryExp(out e);
        }

        private bool parsePrimaryExp(out Expression e)
        {
            if (Current.Kind == TokenKind.Identifier)
                e = new IdentifierExpression(Current);
            else if (Current.Kind == TokenKind.Null)
                e = new NullExpression(Current);
            else if (Current.Kind == TokenKind.True)
                e = new BooleanExpression(Current, true);
            else if (Current.Kind == TokenKind.False)
                e = new BooleanExpression(Current, false);
            else if (Current.Kind == TokenKind.IntegerLiteral)
                e = new IntegerExpression(Current);
            else if (Current.Kind == TokenKind.DecimalLiteral)
                e = new DecimalExpression(Current);
            else if (Current.Kind == TokenKind.StringLiteral)
                e = new StringExpression(Current);
            else if (Current.Kind == TokenKind.BracketOpen)
            {
                e = null;
                Token tok = Current;
                if (!AdvanceOrError("Expected expression after '['")) return false;

                List<Expression> elements = new List<Expression>();
                while (Current.Kind != TokenKind.BracketClose & !Eof())
                {
                    Expression element;
                    if (!parseExpression(out element)) return false;

                    elements.Add(element);
                    if (Current.Kind == TokenKind.BracketClose) break;

                    // Expect a ',' after each expression:
                    if (!Check(TokenKind.Comma)) return false;
                    if (!AdvanceOrError("Expected expression after ','")) return false;
                }
                if (!Check(TokenKind.BracketClose)) return false;

                e = new ListExpression(tok, elements);
            }
            else if (Current.Kind == TokenKind.ParenOpen)
            {
                e = null;
                if (!AdvanceOrError("Expected expression after '('")) return false;
                if (!parseExpression(out e)) return false;
                if (!Check(TokenKind.ParenClose)) return false;
            }
            else
            {
                e = null;
                Error("Unexpected token '{0}'", Current);
                return false;
            }

            Advance();
            return true;
        }

        private Token Current { get { return _lastToken = _tokens.Current; } }
        private Token LastToken { get { return _lastToken; } }
        private int Position { get { return _position; } }

        private bool Eof() { return _eof; }
        private bool Advance()
        {
            if (_eof) return false;

            bool havenext = _tokens.MoveNext();
            if (!havenext) _eof = true;
            ++_position;
            return havenext;
        }

        private bool AdvanceOrError(string error)
        {
            bool havenext = Advance();
            if (!havenext) return Error(error);
            return havenext;
        }

        private bool Check(TokenKind tokenKind)
        {
            if (Eof())
                return Error("Unexpected end of expression");
            if (Current.Kind != tokenKind)
                return Error("Expected {0} but found {1}", Token.kindToString(tokenKind), Current);
            return true;
        }

        private bool Error(string error)
        {
            _errors.Add(new ParserError(_lastToken, error));
            return false;
        }

        private bool Error(string errorFormat, params object[] args)
        {
            Error(String.Format(errorFormat, args));
            return false;
        }
    }
}
