using System;
using System.Collections.Generic;
using System.Linq;
using System.Text; 

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public static class CompilationUnitSyntaxExtensions
    {
        public static CompilationUnitSyntax AddUsings(this CompilationUnitSyntax unit, IEnumerable<string> usings, string currentNamespace)
        {
            usings = usings.Where(x => !currentNamespace.StartsWith(x)).OrderBy(x => x).Distinct();
            return unit.AddUsings(usings.Select(x => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(x))).ToArray());
        }
    }
}
