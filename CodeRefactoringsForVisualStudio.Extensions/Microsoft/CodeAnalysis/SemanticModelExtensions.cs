using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    public static class SemanticModelExtensions
    {
        public static IEnumerable<ISymbol> LookupAccessibleSymbols(this SemanticModel semanticModel, int position, INamespaceOrTypeSymbol container = null)
        {
            var symbols = semanticModel.LookupSymbols(position, container);
            var localSymbols = symbols.Where(symbol => !symbol.IsInaccessibleLocal(position));
            return localSymbols;
        }
    }
}
