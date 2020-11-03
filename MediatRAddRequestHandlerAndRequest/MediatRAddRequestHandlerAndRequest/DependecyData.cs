using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MediatRAddRequestHandlerAndRequest
{
    internal sealed class DependecyData : IEqualityComparer<DependecyData>
    {
        public string Using { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }


        public static async Task<IEnumerable<DependecyData>> ExtractContextDependencies(Solution solution, MemberDeclarationSyntax member, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(member.SyntaxTree);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var memberSymbol = semanticModel.GetDeclaredSymbol(member);

            var result = new List<DependecyData>();
            var identifierNameSyntaxes = member.DescendantNodes().OfType<IdentifierNameSyntax>().ToList();
            foreach (var item in identifierNameSyntaxes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(item);
                var symbol = symbolInfo.Symbol;
                if (symbol != null)
                {
                    if (symbol is ILocalSymbol localSymbol)
                    {
                        continue;
                    }

                    if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingSymbol, memberSymbol.ContainingSymbol))
                    {
                        continue;
                    }

                    var typeInfo = semanticModel.GetTypeInfo(item);
                    var type = typeInfo.Type;

                    if ((type != null) && (type.IsImplicitlyDeclared == false))
                    {
                        var dependecyData = new DependecyData()
                        {
                            Name = symbol.Name.ToString(),
                            Type = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                            Using = type.ContainingNamespace.ToString(),
                        };

                        result.Add(dependecyData);
                    }
                }
            }

            return result.Distinct();
        }



        public bool Equals(DependecyData x, DependecyData y)
        {
            return x.Using == y.Using &&
                  x.Type == y.Type;
        }
        public int GetHashCode(DependecyData obj)
        {
            int hashCode = -80762068;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Using);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            return hashCode;
        }
    }
}
