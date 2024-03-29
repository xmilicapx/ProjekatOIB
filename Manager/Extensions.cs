﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manager
{
    public static class Extensions
    {
        public static string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        public static string RemoveNullTerminator(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !c.Equals('\0'))
                .ToArray());
        }
            
    }
}
