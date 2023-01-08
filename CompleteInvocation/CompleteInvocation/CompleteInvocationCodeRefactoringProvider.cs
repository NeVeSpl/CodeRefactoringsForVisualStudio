using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CompleteInvocation
{
    [ExportCompletionProvider(nameof(CompleteInvocationCodeRefactoringProvider), LanguageNames.CSharp)]
    internal class CompleteInvocationCodeRefactoringProvider : CompletionProvider
    {
        private readonly CompletionItemRules vipRules;

        public CompleteInvocationCodeRefactoringProvider()
        {
            vipRules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect).WithSelectionBehavior(CompletionItemSelectionBehavior.HardSelection);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            if (!document.SupportsSemanticModel || !document.SupportsSyntaxTree) return;
            
            var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var tokenAtCursor = rootNode.FindToken(context.Position - 1);
            if (!tokenAtCursor.IsKind(SyntaxKind.OpenParenToken)) return;

            var argumentList = tokenAtCursor.Parent as ArgumentListSyntax;
            if (argumentList == null || argumentList.Arguments.Any()) return;

            var expression = argumentList.Parent as ExpressionSyntax;
            if (!(expression is InvocationExpressionSyntax) && !(expression is ObjectCreationExpressionSyntax)) return;

            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var symbolInfo = semanticModel.GetSymbolInfo(expression);
            var methodSymbols = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().Where(x => x.Parameters.Count() > 1).ToArray();

            if (methodSymbols.Length == 0) return;

            var accessibleSymbols = semanticModel.LookupAccessibleSymbols(context.Position - 1).ToArray();
            var candidates = accessibleSymbols.Select(x => CandidateAccessor.Create(x)).Where(x => x != null).ToArray();

            if (candidates.Length == 0) return;

            var groupedCandidates = candidates.GroupBy(x => x.Type, SymbolEqualityComparer.Default).ToArray();

            foreach (var methodSymbol in methodSymbols)
            {
                var selectedArguments = new List<string>(methodSymbol.Parameters.Count());

                foreach (var parameter in methodSymbol.Parameters)
                {
                    var candidatesForParameter = groupedCandidates.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.Key, parameter.Type));
                    if (candidatesForParameter == null) goto Exit;

                    foreach (var candidate in candidatesForParameter)
                    {
                        candidate.Score = parameter.Name.ApproximatelyEquals(candidate.Name);
                    }

                    var name = candidatesForParameter.OrderByDescending(x => x.Score).FirstOrDefault().Name;
                    selectedArguments.Add(name);
                }

                var invocation = string.Join(", ", selectedArguments);

                context.AddItem(CompletionItem.Create(invocation, rules: vipRules));
            Exit:
                ;
            }       
        }

        public class CandidateAccessor
        {
            public string Name { get; }
            public ITypeSymbol Type { get; }
            public double Score { get; set; }

            private CandidateAccessor(string name, ITypeSymbol type)
            {
                Name = name;
                Type = type;
            }

            public static CandidateAccessor Create(ISymbol symbol)
            {
                switch(symbol)
                {
                    case ILocalSymbol localSymbol:
                        return new CandidateAccessor(symbol.Name, localSymbol.Type);
                    case IParameterSymbol parameterSymbol:
                        return new CandidateAccessor(symbol.Name, parameterSymbol.Type);
                    case IFieldSymbol fieldSymbol:
                        return new CandidateAccessor(symbol.Name, fieldSymbol.Type);
                }
                return null;
            }
        }
    }
}
