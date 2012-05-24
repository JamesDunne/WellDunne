using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WellDunne.Expressions;
using System.IO;

namespace WellDunne
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Fails(@"");

                Fails(@"(");

                Fails(@"(a");

                Succeeds(@"(a)", @"a");

                Succeeds(
                    @"a or ((b and c) or (d eq 'hello'))",
                    @"(a or ((b and c) or (d eq 'hello')))"
                );

                Succeeds(
                    @"DisplayName like 'ext%'",
                    @"(DisplayName like 'ext%')"
                );

                Succeeds(
                    @"_col1 in [1,2,3]",
                    @"(_col1 in [1,2,3])"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("failed:   " + ex.Message);
            }
        }

        static void Fails(string input)
        {
            Console.WriteLine();
            Console.WriteLine("input:    \"{0}\"", input);
            Console.WriteLine("expected to fail");

            Expression result;
            var parser = new Parser(new Lexer(new StringReader(input)));
            if (!parser.ParseExpression(out result))
                goto passed;

            string output = result.ToString();
            Console.WriteLine("output:   \"{0}\"", output);
            Console.WriteLine("failed");
            return;
        passed:
            Console.WriteLine("passed");
        }

        static void Succeeds(string input, string expected)
        {
            Console.WriteLine();
            Console.WriteLine("input:    \"{0}\"", input);

            Expression result;
            var parser = new Parser(new Lexer(new StringReader(input)));
            if (!parser.ParseExpression(out result))
                throw new Exception("Parser failed!");

            string output = result.ToString();
            Console.WriteLine("output:   \"{0}\"", output);

            Console.WriteLine("expected: \"{0}\"", expected);

            if (!String.Equals(expected, output))
                throw new Exception("Expected is not equal to actual" + Environment.NewLine + expected + Environment.NewLine + output);
            Console.WriteLine("passed");
        }
    }
}
