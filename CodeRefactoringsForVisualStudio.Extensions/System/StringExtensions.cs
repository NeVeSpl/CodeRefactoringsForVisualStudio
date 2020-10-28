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

        public static string ToUpperFirst(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            char[] a = text.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static string ToLowerFirst(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            char[] a = text.ToCharArray();
            a[0] = char.ToLower(a[0]);
            return new string(a);
        }

        public static string RemoveSufix(this string text, params string[] suffixes)
        {
            foreach (var sufix in suffixes)
            {
                if (text.EndsWith(sufix, StringComparison.Ordinal))
                {
                    return text.Substring(0, text.Length - sufix.Length);
                }
            }
            return text;
        }
    }
}
