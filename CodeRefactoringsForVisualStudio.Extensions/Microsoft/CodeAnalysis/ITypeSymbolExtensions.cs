using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    public static class ITypeSymbolExtensions
    {
        public static IEnumerable<string> GetUsings(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol.ContainingNamespace != null)
            {
                yield return typeSymbol.ContainingNamespace.ToString();
            }

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                {
                    foreach (var item in typeArgument.GetUsings())
                    {
                        yield return item;
                    }
                }    
            }
            
            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                foreach (var item in arrayTypeSymbol.ElementType.GetUsings())
                {
                    yield return item;
                }
            }
        }
    }
}