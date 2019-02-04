using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Refactorings.ConvertToFullWPFProperty
{
    public static class FieldNameGenerator
    {
        public static string Generate(string name, char? prefix = null)
        {
            string result = string.Empty;

            if (!string.IsNullOrWhiteSpace(name))
            {
                if (prefix.HasValue)
                {                   
                    if (char.IsLetter(prefix.Value))
                    {
                        result = prefix.Value.ToString() + char.ToUpper(name[0]) + name.Substring(1);
                    }
                    else
                    {
                        result = prefix.Value.ToString() + char.ToLower(name[0]) + name.Substring(1);
                    }                   
                }
                else
                {
                    result = char.ToLower(name[0]) + name.Substring(1);
                }
            }

            return result;
        }
    }
}
