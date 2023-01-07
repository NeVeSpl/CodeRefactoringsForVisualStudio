using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenerateMapping.Model
{
    enum WriteLevel { Constructor, Init, Ordinary, NotApplicable }
    enum AccessLevel { Private, Public }
    enum Side { Left, Right }

    internal static class AccessorsExtractor
    {
        public static  (IEnumerable<Accessor> outputs, IEnumerable<Accessor> inputs) GetAccessors(SemanticModel semanticModel, CSharpSyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            var methodDeclaration = syntaxNode.FirstParentOrSelfOfType<BaseMethodDeclarationSyntax>();
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);

            IEnumerable<Accessor> leftAccessors = GetLeftAccessor(methodSymbol).ToList();
            IEnumerable<Accessor> rightAccessors = GetRightAccessors(methodSymbol).ToList();

            if (syntaxNode is ObjectCreationExpressionSyntax objectCreation)
            {
                var typeInfo = semanticModel.GetTypeInfo(objectCreation);
                var accessLevel = SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, typeInfo.Type) ? AccessLevel.Private : AccessLevel.Public;

                leftAccessors = new[] { new Accessor(typeInfo.Type, Accessor.SpecialNameReturnType, accessLevel, Side.Left, WriteLevel.Init) };
            }

            return (leftAccessors, rightAccessors);
        }
       

        private static IEnumerable<Accessor> GetLeftAccessor(IMethodSymbol methodSymbol)
        {
            if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Void && methodSymbol.IsStatic == false)
            {
                if (methodSymbol.Parameters.Count() > 0 && methodSymbol.Parameters.All(x => x.RefKind == RefKind.Out))
                {
                    var outputParameters = methodSymbol.Parameters.Where(x => x.RefKind == RefKind.Out);

                    foreach (var parameter in outputParameters)
                    {
                        yield return new Accessor(parameter.Type, parameter.Name, AccessLevel.Public, Side.Left, WriteLevel.NotApplicable);
                    }
                }
                else
                {
                    var writeLevel = methodSymbol.MethodKind == MethodKind.Constructor ? WriteLevel.Constructor : WriteLevel.Ordinary;
                    var accessLevel = AccessLevel.Private;

                    yield return new Accessor(methodSymbol.ContainingType, Accessor.SpecialNameThis, accessLevel, Side.Left, writeLevel);
                }                
            }
            else
            {                
                var writeLevel = WriteLevel.Init;
                var accessLevel = SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, methodSymbol.ReturnType) ? AccessLevel.Private : AccessLevel.Public;

                yield return new Accessor(methodSymbol.ReturnType, Accessor.SpecialNameReturnType, accessLevel, Side.Left, writeLevel);
            }
        }
        private static IEnumerable<Accessor> GetRightAccessors(IMethodSymbol methodSymbol)
        {
            var inputParameters = methodSymbol.Parameters.Where(x => x.RefKind != RefKind.Out);

            if (inputParameters.Any())
            {
                foreach (var parameter in inputParameters)
                {
                    var accessLevel = SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, parameter.Type) ? AccessLevel.Private : AccessLevel.Public;

                    yield return new Accessor(parameter.Type, parameter.Name, accessLevel, Side.Right, WriteLevel.NotApplicable);
                }
            }
            else
            {
                var accessLevel = AccessLevel.Private;

                yield return new Accessor(methodSymbol.ContainingType, Accessor.SpecialNameThis, accessLevel, Side.Right, WriteLevel.NotApplicable);
            }
        }
    }
}