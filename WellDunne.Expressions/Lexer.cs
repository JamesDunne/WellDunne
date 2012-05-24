using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WellDunne.Expressions
{
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

                if (c == '_' || Char.IsLetter(c))
                {
                    // Start consuming an identifer:
                    var sb = new StringBuilder(8);
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
                    if (ident == "null")
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
                else if (Char.IsDigit(c))
                {
                    // Start consuming a numeric literal:
                    var sb = new StringBuilder(8);
                    sb.Append(Read());
                    while (!EndOfStream())
                    {
                        char c2 = Peek();
                        // HACK(jsd): Should create a new state to parse decimals properly.
                        if (c2 == '.' || Char.IsDigit(c2))
                            sb.Append(Read());
                        else
                            break;
                    }
                    yield return new Token(TokenKind.Identifier, position, sb.ToString());
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
}
