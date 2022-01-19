using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    public static class ISymbolExtensions
    {
        public static bool IsCompilerGenerated(this ISymbol symbol)
        {
            var attributes = symbol.GetAttributes();

            if (attributes.Any(x => x.AttributeClass.Name == nameof(CompilerGeneratedAttribute)))
            {
                return true;
            }

            return symbol.IsImplicitlyDeclared;
        }
    }
}
