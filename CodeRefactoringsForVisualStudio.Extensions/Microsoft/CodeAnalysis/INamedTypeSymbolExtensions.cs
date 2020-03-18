using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis
{
    public static class INamedTypeSymbolExtensions
    {
        public static async Task<string> DetermineMethodNameUsedToNotifyThatPropertyWasChanged(this INamedTypeSymbol typeSymbol, Solution solution)
        {
            String result = "OnPropertyChanged";           

            var typesInInheritanceHierarchy = new List<INamedTypeSymbol>();
            var currentType = typeSymbol;     
            while ((currentType != null) && (!currentType.ContainingNamespace?.ToString().StartsWith("System.") == true))
            {
                typesInInheritanceHierarchy.Add(currentType);
                currentType = currentType.BaseType;
            }

            foreach (INamedTypeSymbol type in typesInInheritanceHierarchy)
            {
                var methodSymbols = type.GetMembers().Where(x => !x.IsImplicitlyDeclared).OfType<IMethodSymbol>().Where(x => x.MethodKind == MethodKind.Ordinary);
                foreach (ISymbol methodSymbol in methodSymbols)
                {
                    foreach (var syntaxReference in methodSymbol.DeclaringSyntaxReferences)
                    {
                        var methodNode = await syntaxReference.GetSyntaxAsync().ConfigureAwait(false) as MethodDeclarationSyntax;
                        var invocations = methodNode.DescendantNodes().OfType<InvocationExpressionSyntax>();
                        foreach (InvocationExpressionSyntax invocation in invocations)
                        {
                            if (invocation.Expression is IdentifierNameSyntax idSyntaxt)
                            {
                                if (idSyntaxt.Identifier.ValueText == "PropertyChanged")
                                {
                                    result = methodSymbol.Name;
                                    return result;
                                }
                            }
                        }
                    }
                }
            }
                /* Old slower implementation
                  foreach (INamedTypeSymbol interfaceSymbol in typeSymbol.AllInterfaces)
                  {
                      if (interfaceSymbol.Name == "INotifyPropertyChanged" && String.Equals(interfaceSymbol?.ContainingNamespace.ToString(), "System.ComponentModel"))
                      {                   
                          ISymbol propertyChangedEventSymbol = interfaceSymbol.GetMembers("PropertyChanged").First();
                          foreach (Location location in type.Locations)
                          {
                              if (location.SourceTree != null)
                              {
                                  var document = solution.GetDocument(location.SourceTree);                                
                                  var setOfDocuments =  ImmutableHashSet.Create( document);

                                  IEnumerable<SymbolCallerInfo> callers = await SymbolFinder.FindCallersAsync(propertyChangedEventSymbol, solution, setOfDocuments).ConfigureAwait(false);

                                  foreach (SymbolCallerInfo caller in callers)
                                  {
                                      if ((caller.CallingSymbol is IMethodSymbol methodSymbol)  && (methodSymbol.MethodKind == MethodKind.Ordinary))
                                      {
                                          result = caller.CallingSymbol.Name;
                                      }
                                  }                               
                              }
                          }

                       }
                  } */            

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
