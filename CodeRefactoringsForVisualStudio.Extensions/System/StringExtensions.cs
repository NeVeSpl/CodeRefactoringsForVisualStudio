using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class StringExtensions
    {
        public static bool HasPrefix(this String text)
        {
            if (text.Length > 1)
            {
                if (text[0] == '_') return true;
                return char.IsLower(text[0]) && char.IsUpper(text[1]);
            }
            return false;            
        }

        public static string WithoutPrefix(this String text)
        {
            if (text.HasPrefix())
            {
                text = text.Remove(0, 1);
            }
            return text;
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

        public static string RemovePostfix(this string text, params string[] postfixes)
        {
            foreach (var sufix in postfixes)
            {
                if (text.EndsWith(sufix, StringComparison.Ordinal))
                {
                    return text.Substring(0, text.Length - sufix.Length);
                }
            }
            return text;
        }




        public static IEnumerable<string> SplitStringIntoSeparateWords(this string selectedText)
        {
            int wordFirstIndex = 0;
            for (int i = 0; i < selectedText.Length; ++i)
            {
                if (!char.IsLetterOrDigit(selectedText[i]))
                {
                    int wordLength = i - wordFirstIndex;
                    if (wordLength > 0)
                    {
                        yield return selectedText.Substring(wordFirstIndex, wordLength);
                    }
                    wordFirstIndex = i + 1;
                }
                if (char.IsUpper(selectedText[i]))
                {
                    int wordLength = i - wordFirstIndex;
                    if (wordLength > 0)
                    {
                        yield return selectedText.Substring(wordFirstIndex, wordLength);
                    }
                    wordFirstIndex = i;
                }
            }
            int remainderLength = selectedText.Length - wordFirstIndex;
            if (remainderLength > 0)
            {
                yield return selectedText.Substring(wordFirstIndex, remainderLength);
            }
        }





        public static double ApproximatelyEquals(this string a, string b)
        {
            if (string.Equals(a, b)) return 1.0;

            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return 0.9;

            var a_wp = a.WithoutPrefix();
            var b_wp = b.WithoutPrefix();
            if (string.Equals(a_wp, b_wp, StringComparison.OrdinalIgnoreCase)) return 0.8;

            var a_s = a_wp.SplitStringIntoSeparateWords();
            var b_s = b_wp.SplitStringIntoSeparateWords();
            if (a_s.SequenceEqual(b_s, StringComparer.OrdinalIgnoreCase)) return 0.7;

            return 0.0;
        }

    }
}
