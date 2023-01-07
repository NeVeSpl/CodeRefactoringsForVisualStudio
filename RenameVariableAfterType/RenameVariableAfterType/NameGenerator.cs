using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Pluralize.NET;

namespace RenameVariableAfterType
{
    internal static class NameGenerator
    {
        public static string GenerateNewNameFromType(ITypeSymbol typeSymbol)
        {
            var type = typeSymbol;
            bool isCollection = false;
            var genericNames = new List<string>();
            while (type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType && namedTypeSymbol.TypeArguments.Length > 0)
            {
                isCollection |= type.IsCollection();
                isCollection |= type.IsEnumerable();
                if (type.ContainingNamespace?.ToString().StartsWith("System.Collections") == true)
                {
                    isCollection = true;                    
                }
                else
                {
                    if (namedTypeSymbol.Name != "Task")
                    {
                        genericNames.Add(namedTypeSymbol.Name);
                    }
                }
                type = namedTypeSymbol.TypeArguments.First();
            }

            if (type is IArrayTypeSymbol arrayTypeSymbol)
            {
                type = arrayTypeSymbol.ElementType;
                isCollection = true;
            }

            string name = type.Name;

            if ((name.Length > 1) && (name[0] == 'I') && (char.IsUpper(name[1])))
            {
                name = name.Substring(1);
            }
            var words = name.SplitStringIntoSeparateWords().ToList();

            if (words.Any())
            {
                if (isCollection)
                {
                    words[words.Count - 1] = Pluralize(words.Last());
                }
                words[0] = words.First().ToLowerFirst();
            }
            
            string newName = string.Join("", words);

            if (genericNames.Any())
            {
                string prefix = string.Join("_", genericNames.Select(x => x.ToLowerFirst())) + "_";
                newName = prefix + newName;
            }

            return newName;
        }

        private static string Pluralize(string word)
        {
            string pluralForm = word;
            try
            {
                IPluralize pluralizer = new Pluralizer();
                pluralForm = pluralizer.Pluralize(word);
            }
            catch
            {
                // sometimes VS has problems with loading Pluralize dll
                pluralForm = GetPlural(word);
            }
            return pluralForm;
        }

        // source : https://stackoverflow.com/a/16199962/1147478
        private static string GetPlural(string singular)
        {
            string CONSONANTS = "bcdfghjklmnpqrstvwxz";

            switch (singular)
            {
                case "Person":
                    return "People";
                case "Trash":
                    return "Trash";
                case "Life":
                    return "Lives";
                case "Man":
                    return "Men";
                case "Woman":
                    return "Women";
                case "Child":
                    return "Children";
                case "Foot":
                    return "Feet";
                case "Tooth":
                    return "Teeth";
                case "Dozen":
                    return "Dozen";
                case "Hundred":
                    return "Hundred";
                case "Thousand":
                    return "Thousand";
                case "Million":
                    return "Million";
                case "Datum":
                    return "Data";
                case "Criterion":
                    return "Criteria";
                case "Analysis":
                    return "Analyses";
                case "Fungus":
                    return "Fungi";
                case "Index":
                    return "Indices";
                case "Matrix":
                    return "Matrices";
                case "Settings":
                    return "Settings";
                case "UserSettings":
                    return "UserSettings";
                default:
                    // Handle ending with "o" (if preceeded by a consonant, end with -es, otherwise -s: Potatoes and Radios)
                    if (singular.EndsWith("o") && CONSONANTS.Contains(singular[singular.Length - 2].ToString()))
                    {
                        return singular + "es";
                    }
                    // Handle ending with "y" (if preceeded by a consonant, end with -ies, otherwise -s: Companies and Trays)
                    if (singular.EndsWith("y") && CONSONANTS.Contains(singular[singular.Length - 2].ToString()))
                    {
                        return singular.Substring(0, singular.Length - 1) + "ies";
                    }
                    // Ends with a whistling sound: boxes, buzzes, churches, passes
                    if (singular.EndsWith("s") || singular.EndsWith("sh") || singular.EndsWith("ch") || singular.EndsWith("x") || singular.EndsWith("z"))
                    {
                        return singular + "es";
                    }
                    return singular + "s";

            }
        }




        static string[] prefixes = new string[] { "get", "set", "invoke", "calculate", "compute" };
        static string[] genericNames = new string[] { "ToList", "ToArray" };
        public static string GenerateNewNameFromExpression(ExpressionSyntax expressionSyntax)
        {
            string expression = null;
            if (expressionSyntax is InvocationExpressionSyntax invocationExpressionSyntax)
            {
                expression = invocationExpressionSyntax.Expression.ToString();
            }
            if (expressionSyntax is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
            {
                expression = memberAccessExpressionSyntax.ToString();
            }
            if (string.IsNullOrEmpty(expression))
            {
                return null;
            }

            string[] words = expression.Split('.').Where(x => !string.IsNullOrEmpty(x)).Select(x => ExtractName(x)).ToArray();
            string lastWord = words.Reverse().Where(x => genericNames.Contains(x) == false ).FirstOrDefault();

            if (string.IsNullOrEmpty(lastWord)) return null;

            foreach (var prefix in prefixes)
            {
                if (lastWord.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    lastWord = lastWord.Substring(prefix.Length);
                }
            }

            if (string.IsNullOrEmpty(lastWord)) return null;
           
             return lastWord.ToLowerFirst();           
        }

        private static string ExtractName(string text)
        {
            var index = text.IndexOf('(');
            if (index > -1)
            {
                return text.Substring(0, index);
            }
            return text;
        }
    }
}
