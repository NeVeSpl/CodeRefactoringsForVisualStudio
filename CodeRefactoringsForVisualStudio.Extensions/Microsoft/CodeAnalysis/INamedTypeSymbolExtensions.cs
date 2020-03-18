using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    public static class INamedTypeSymbolExtensions
    {
        public static async Task<string> DetermineMethodNameUsedToNotifyThatPropertyWasChanged(this INamedTypeSymbol typeSymbol, Solution solution)
        {
            String result = "OnPropertyChanged";

            //IAssemblySymbol assemblySymbol = typeSymbol.ContainingAssembly;

            var typesInInheritanceHierarchy = new HashSet<INamedTypeSymbol>();

            var currentType = typeSymbol;
            while (currentType != null)
            {
                typesInInheritanceHierarchy.Add(currentType);
                currentType = currentType.BaseType;
            }

            foreach (INamedTypeSymbol interfaceSymbol in typeSymbol.AllInterfaces)
            {
                if (interfaceSymbol.Name == "INotifyPropertyChanged" && String.Equals(interfaceSymbol?.ContainingNamespace.ToString(), "System.ComponentModel"))
                {
                    ISymbol propertyChangedEventSymbol = interfaceSymbol.GetMembers("PropertyChanged").First();
                    IEnumerable<SymbolCallerInfo> callers = await SymbolFinder.FindCallersAsync(propertyChangedEventSymbol, solution).ConfigureAwait(false);

                    foreach (SymbolCallerInfo caller in callers)
                    {
                        if (typesInInheritanceHierarchy.Contains(caller.CallingSymbol.ContainingType))
                        {
                            result = caller.CallingSymbol.Name;
                        }
                    }
                }
            }

            return result;
        }

        public static char? DetermineBackingFiledPrefix(this INamedTypeSymbol typeSymbol)
        {
            char? result = null;

            IEnumerable<ISymbol> backingFileds = typeSymbol.GetMembers().Where(x => x.Kind == SymbolKind.Field).Where(x => x.IsImplicitlyDeclared == false);

            if (backingFileds.Any())
            {
                if (backingFileds.First().Name?.HasPrefix() == true)
                {
                    char candidateForPrefix = backingFileds.First().Name[0];
                    if (backingFileds.All(x => x.Name[0] == candidateForPrefix))
                    {
                        result = candidateForPrefix;
                    }
                }
            }

            return result;
        }       
    }
}
