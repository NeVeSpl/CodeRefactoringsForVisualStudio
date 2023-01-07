using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenerateMapping.Model
{
    enum WriteLevel { Constructor, Init, Ordinary, NotApplicable } 
    enum Side { Left, Right }

    internal static class AccessorsExtractor
    {
        public static  (IEnumerable<Accessor> outputs, IEnumerable<Accessor> inputs) GetAccessors(SemanticModel semanticModel, CSharpSyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            var methodDeclaration = syntaxNode.FirstParentOrSelfOfType<BaseMethodDeclarationSyntax>();
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);

            var accessorFactory = new AccessorFactory(methodSymbol.ContainingType, semanticModel, methodDeclaration.SpanStart);
            IEnumerable<Accessor> leftAccessors = GetLeftAccessor(methodSymbol, accessorFactory).ToList();
            IEnumerable<Accessor> rightAccessors = GetRightAccessors(methodSymbol, accessorFactory).ToList();

            if (syntaxNode is ObjectCreationExpressionSyntax objectCreation)
            {              
                var typeInfo = semanticModel.GetTypeInfo(objectCreation);
                var publicOnly = SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, typeInfo.Type) ? false : true;
                var accessibleSymbols = semanticModel.LookupAccessibleSymbols(methodDeclaration.SpanStart, typeInfo.Type);

                leftAccessors = new[] { new Accessor(typeInfo.Type, Accessor.SpecialNameReturnType, accessibleSymbols, publicOnly, Side.Left, WriteLevel.Init) };
            }

            return (leftAccessors, rightAccessors);
        }
       

        private static IEnumerable<Accessor> GetLeftAccessor(IMethodSymbol methodSymbol, AccessorFactory accessorFactory)
        {
            if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Void && methodSymbol.IsStatic == false)
            {
                if (methodSymbol.Parameters.Count() > 0 && methodSymbol.Parameters.All(x => x.RefKind == RefKind.Out))
                {
                    var outputParameters = methodSymbol.Parameters.Where(x => x.RefKind == RefKind.Out);

                    foreach (var parameter in outputParameters)
                    {                       
                        yield return accessorFactory.CreateAccessor(parameter.Type, parameter.Name, Side.Left, WriteLevel.NotApplicable);
                    }
                }
                else
                {
                    var writeLevel = methodSymbol.MethodKind == MethodKind.Constructor ? WriteLevel.Constructor : WriteLevel.Ordinary;                    
                    yield return accessorFactory.CreateAccessor(methodSymbol.ContainingType, Accessor.SpecialNameThis, Side.Left, writeLevel);
                }                
            }
            else
            {
                yield return accessorFactory.CreateAccessor(methodSymbol.ReturnType, Accessor.SpecialNameReturnType, Side.Left, WriteLevel.Init);
            }
        }
        private static IEnumerable<Accessor> GetRightAccessors(IMethodSymbol methodSymbol, AccessorFactory accessorFactory)
        {
            var inputParameters = methodSymbol.Parameters.Where(x => x.RefKind != RefKind.Out);

            if (inputParameters.Any())
            {
                foreach (var parameter in inputParameters)
                {
                    yield return accessorFactory.CreateAccessor(parameter.Type, parameter.Name, Side.Right, WriteLevel.NotApplicable);
                }
            }
            else
            {                
                yield return accessorFactory.CreateAccessor(methodSymbol.ContainingType, Accessor.SpecialNameThis, Side.Right, WriteLevel.NotApplicable);
            }
        }

        class AccessorFactory
        {
            private readonly ITypeSymbol containingType;
            private readonly SemanticModel semanticModel;
            private readonly int position;

            public AccessorFactory(ITypeSymbol containingType, SemanticModel semanticModel, int position)
            {
                this.containingType = containingType;
                this.semanticModel = semanticModel;
                this.position = position;
            }


            public Accessor CreateAccessor(ITypeSymbol type, string name, Side sideOfAssignment, WriteLevel writeLevel)
            {
                var publicOnly = SymbolEqualityComparer.Default.Equals(containingType, type) ? false : true;
                var accessibleSymbols = semanticModel.LookupAccessibleSymbols(position, type);
                return new Accessor(type, name, accessibleSymbols, publicOnly, sideOfAssignment, writeLevel);
            }
        }
    }
}