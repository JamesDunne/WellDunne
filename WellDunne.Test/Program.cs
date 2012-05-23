using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WellDunne.Expressions;

namespace WellDunne
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser(new Lexer(new System.IO.StringReader(@"a or b")));
            parser.Parse();
        }
    }
}
