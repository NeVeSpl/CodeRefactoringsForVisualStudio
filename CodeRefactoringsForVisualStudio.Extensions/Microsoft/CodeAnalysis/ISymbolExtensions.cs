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


        /// <returns>
        /// Returns true if symbol is a local variable and its declaring syntax node is 
        /// after the current position, false otherwise (including for non-local symbols)
        /// </returns>
        /// https://github.com/dotnet/roslyn/blob/aaee0047096782110b43a87df7d0e85e14b1af68/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/ISymbolExtensions.cs
        public static bool IsInaccessibleLocal(this ISymbol symbol, int position)
        {
            if (symbol.Kind != SymbolKind.Local)
            {
                return false;
            }

            // Implicitly declared locals (with Option Explicit Off in VB) are scoped to the entire
            // method and should always be considered accessible from within the same method.
            if (symbol.IsImplicitlyDeclared)
            {
                return false;
            }

            var declarationSyntax = symbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).FirstOrDefault();
            return declarationSyntax != null && position < declarationSyntax.SpanStart;
        }
    }
}
