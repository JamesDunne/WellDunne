using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WellDunne.Expressions
{
    public sealed class Parser
    {
        private readonly Lexer _lexer;

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
        }

        public void Parse()
        {
            foreach (var token in _lexer.Lex())
            {
                Console.WriteLine("{0}, {1}, {2}", token.Kind, token.Position, token.Value);
            }
        }
    }
}
