﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WellDunne.Expressions
{
    public class IdentifierExpression : Expression
    {
        private readonly Token _token;

        public IdentifierExpression(Token token)
        {
            _token = token;
        }

        public override void WriteTo(TextWriter tw)
        {
            tw.Write(_token.Value);
        }
    }
}