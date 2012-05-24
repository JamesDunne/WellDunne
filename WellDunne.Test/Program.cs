using System;
using System.IO;
using WellDunne.ExpressionLibrary;

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
                Fails(@"a like");
                Fails(@"a in");
                Fails(@"a eq");
                Fails(@"a ne");

                Succeeds(@"(a)", @"a");

                Succeeds(
                    @" a or ((b and c) or (d eq 'hello'))",
                    @"(a or ((b and c) or (d eq 'hello')))"
                );

                Succeeds(
                    @" DisplayName like 'ext%'",
                    @"(DisplayName like 'ext%')"
                );

                Succeeds(
                    @" _col1 in [1,2,3]",
                    @"(_col1 in [1,2,3])"
                );

                Succeeds(
                    @" __ in ['abc','def','ghi']",
                    @"(__ in ['abc','def','ghi'])"
                );

                // One trailing comma at the end of a list is fine:
                Succeeds(
                    @" __ in ['abc',]",
                    @"(__ in ['abc'])"
                );

                Succeeds(
                    @" a in [1,2,3,]",
                    @"(a in [1,2,3])"
                );

                // More than one trailing comma is bad:
                Fails(@"__ in ['abc',,]");

                Succeeds(
                    @"  (a eq 'test') and (b ne 'word')  and (c in [1,4,5,16,17,18,20,22,])",
                    @"(((a eq 'test') and (b ne 'word')) and (c in [1,4,5,16,17,18,20,22]))"
                );

                Succeeds(
                    @" c eq 12",
                    @"(c eq 12)"
                );

                Succeeds(
                    @" d eq 12.0",
                    @"(d eq 12.0)"
                );

                Succeeds(
                    @" e eq true",
                    @"(e eq true)"
                );

                Succeeds(
                    @" e eq false",
                    @"(e eq false)"
                );

                Succeeds(
                    @" f ne null",
                    @"(f ne null)"
                );

                Succeeds(
                    @" f gt g",
                    @"(f gt g)"
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
            throw new Exception("Expected to fail but passed!");
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
