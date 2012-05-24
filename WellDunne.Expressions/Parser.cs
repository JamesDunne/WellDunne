using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WellDunne.Expressions
{
    public sealed class Parser
    {
        private readonly Lexer _lexer;
        private IEnumerator<Token> _tokens;

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            _tokens = _lexer.Lex().GetEnumerator();
        }

        private bool Advance()
        {
            return _tokens.MoveNext();
        }

        private Token Current()
        {
            return _tokens.Current;
        }

        private void debug()
        {
            var token = _tokens.Current;
            Console.WriteLine("{0}, {1}, {2}", token.Kind, token.Position, token.Value);
        }

        public bool ParseExpression(out Expression result)
        {
            using (_tokens)
            {
                return parseExpression(out result);
            }
        }

        public bool parseExpression(out Expression result)
        {
            if (!parsePrimary(out result)) return false;
            return true;
        }

        private bool parseBinary(out Expression result)
        {
            result = null;
#if false
            if (!Advance()) return false;

            Expression l;
            if (!parsePrimary(out l)) return false;

            if (!Advance())
            {
                result = l;
                return true;
            }

            if (Current().Kind != TokenKind.Operator)
            {
            }
#endif
            return false;
        }

        private bool parsePrimary(out Expression result)
        {
            result = null;
            if (!Advance()) return false;

            if (Current().Kind == TokenKind.Identifier)
            {
                result = new IdentifierExpression(Current());
                return true;
            }
            else if (Current().Kind == TokenKind.IntegerLiteral)
            {
                result = new LiteralExpression(Current());
                return true;
            }

            return false;
        }
    }
}
