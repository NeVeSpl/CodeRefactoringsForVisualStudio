using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    public static class StringExtensions
    {
        public static bool HasPrefix(this String text)
        {
            return (text.Length > 1)
                  && (char.IsLower(text[0]) || (text[0] == '_'))
                  && (char.IsUpper(text[1]));
        }
    }
}
