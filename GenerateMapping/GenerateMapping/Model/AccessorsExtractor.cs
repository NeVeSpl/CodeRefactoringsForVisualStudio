using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenerateMapping.Model
{
    internal static class AccessorsExtractor
    {
        public static  (IEnumerable<Accessor> outputs, IEnumerable<Accessor> inputs) GetAccessors(SemanticModel semanticModel, CSharpSyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            var methodDeclaration = syntaxNode.FirstParentOrSelfOfType<BaseMethodDeclarationSyntax>();
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);

            //var l = methodDeclaration.Body.GetLocation();
            //var tt = semanticModel.LookupSymbols(l.SourceSpan.Start);

            //foreach (StatementSyntax S in methodDeclaration.Body.Statements)
            //{
            //    var d = S.GetLeadingTrivia();
            //    var f = S.GetTrailingTrivia();
            //}            

            IEnumerable<Accessor> leftAccessors = GetLeftAccessor(methodSymbol).ToList();
            IEnumerable<Accessor> rightAccessors = GetRightAccessors(methodSymbol).ToList();

            if (syntaxNode is ObjectCreationExpressionSyntax objectCreation)
            {
                var typeInfo = semanticModel.GetTypeInfo(objectCreation);
                leftAccessors = new[] { new Accessor(typeInfo.Type, Accessor.SpecialNameReturnType, true, mustBeRedable: false, mustBeWritable: true) };
            }

            return (leftAccessors, rightAccessors);
        }

       

        private static IEnumerable<Accessor> GetLeftAccessor(IMethodSymbol methodSymbol)
        {
            if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Void && methodSymbol.IsStatic == false)
            {
                yield return new Accessor(methodSymbol.ContainingType, Accessor.SpecialNameThis, false, mustBeRedable: false, mustBeWritable: true);
            }
            else
            {
                yield return new Accessor(methodSymbol.ReturnType, Accessor.SpecialNameReturnType, true, mustBeRedable: false, mustBeWritable: true);
            }
        }
        private static IEnumerable<Accessor> GetRightAccessors(IMethodSymbol methodSymbol)
        {
            if (methodSymbol.Parameters.Any())
            {
                foreach (var parameter in methodSymbol.Parameters)
                {
                    yield return new Accessor(parameter.Type, parameter.Name, true, mustBeRedable: true, mustBeWritable: false);
                }
            }
            else
            {
                yield return new Accessor(methodSymbol.ContainingType, Accessor.SpecialNameThis, false, mustBeRedable: true, mustBeWritable: false);
            }
        }
    }
}