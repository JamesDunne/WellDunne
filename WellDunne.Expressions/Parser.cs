﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WellDunne.Expressions
{
    public sealed class Parser
    {
        private readonly Lexer _lexer;
        private IEnumerator<Token> _tokens;
        private bool _eof;
        private Token _lastToken;

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            _tokens = _lexer.Lex().GetEnumerator();
        }

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
            else if (Current.Kind == TokenKind.ParenOpen)
            {
                e = null;
                if (!AdvanceOrError("Expected expression after '('")) return false;
                if (!parseExpression(out e)) return false;
                if (!check(TokenKind.ParenClose)) return false;
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

        private bool Eof() { return _eof; }
        private bool Advance()
        {
            if (_eof) return false;

            bool havenext = _tokens.MoveNext();
            if (!havenext) _eof = true;
            return havenext;
        }

        private bool AdvanceOrError(string error)
        {
            bool havenext = Advance();
            if (!havenext) return Error(error);
            return havenext;
        }

        private bool check(TokenKind tokenKind)
        {
            if (Current.Kind != tokenKind)
                return Error("Expected {0} but found {1}", Token.kindToString(tokenKind), Current);
            return true;
        }

        private bool Error(string error)
        {
            Console.Error.WriteLine("error(at {0}): {1}", _lastToken.Position + 1, error);
            return false;
        }

        private bool Error(string errorFormat, params object[] args)
        {
            Error(String.Format(errorFormat, args));
            return false;
        }

        private void debug()
        {
            var token = Current;
            Console.WriteLine("{0}, {1}, {2}", token.Kind, token.Position, token.Value);
        }
    }
}
