using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace IntroduceParameterObject
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(IntroduceParameterObjectCodeRefactoringProvider)), Shared]
    internal class IntroduceParameterObjectCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            //var parameters = rootNode.ExtractSelectedNodesOfType<ParameterSyntax>(context.Span);
            var methodDeclarations = rootNode.ExtractSelectedNodesOfType<MethodDeclarationSyntax>(context.Span, true);

            //if (parameters.Any() == false)
            //{
                if (methodDeclarations.Any() == false)
                {
                    return;
                }
                var parameters = methodDeclarations.First().ParameterList.Parameters;
            //}

            var action = CodeAction.Create("Introduce parameter object", c => IntroduceParameterObject(context.Document, methodDeclarations.First(), parameters, c));
            context.RegisterRefactoring(action);
        }

        private async Task<Solution> IntroduceParameterObject(Document document, MethodDeclarationSyntax method, IEnumerable<ParameterSyntax> parameters, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            IMethodSymbol methodSymbol = semanticModel.GetDeclaredSymbol(method, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var parameterObject = new ParameterObjectGenerator.ParameterObject(methodSymbol, parameters, semanticModel);
            var parameterObjectSyntax = ParameterObjectGenerator.CreateParameterObjectClass(parameterObject, parameters);

            cancellationToken.ThrowIfCancellationRequested();
            
            var updatedInvocations = await UpdateMethodInvocations(document.Project.Solution, methodSymbol, parameterObject.Name, cancellationToken).ConfigureAwait(false);
            var nodesToReplace = new List<SyntaxNodeToReplace>(updatedInvocations);

            cancellationToken.ThrowIfCancellationRequested();

            var updatedMethod = UpdateMethodBody(method, parameters, parameterObject.Name, semanticModel);
            updatedMethod = UpdateMethodParameterList(updatedMethod, parameters, parameterObject.Name);          
            nodesToReplace.Add(new SyntaxNodeToReplace(method, updatedMethod));

            cancellationToken.ThrowIfCancellationRequested();

            Solution updatedSolution = await ReplaceNodes(document.Project.Solution, nodesToReplace, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var project = updatedSolution.GetProject(document.Project.Id);
            var parameterObjectDocument = project.AddDocument(parameterObject.Name + ".cs", parameterObjectSyntax, document.Folders);

            cancellationToken.ThrowIfCancellationRequested();

            return parameterObjectDocument.Project.Solution;
        }

        private async Task<List<SyntaxNodeToReplace>> UpdateMethodInvocations(Solution solution, IMethodSymbol methodSymbol, string parameterObjectName, CancellationToken cancellationToken)
        {
            var nodesToReplace = new List<SyntaxNodeToReplace>();
            IEnumerable<SymbolCallerInfo> callers = await SymbolFinder.FindCallersAsync(methodSymbol, solution, cancellationToken).ConfigureAwait(false);
            foreach (var caller in callers)
            {
                foreach (var location in caller.Locations)
                {
                    var node = location.SourceTree.GetRoot()?.ExtractSelectedNodesOfType<InvocationExpressionSyntax>(location.SourceSpan).FirstOrDefault();
                    if (node != null)
                    {
                        var parameterObjectConstructorInvocation = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(parameterObjectName)).WithArgumentList(node.ArgumentList);
                        var updatedArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(parameterObjectConstructorInvocation)));
                        var updatedNode = node.WithArgumentList(updatedArgumentList);
                        nodesToReplace.Add(new SyntaxNodeToReplace(node, updatedNode));
                    }
                }
            }
            return nodesToReplace;
        }
        private MethodDeclarationSyntax UpdateMethodParameterList(MethodDeclarationSyntax method, IEnumerable<ParameterSyntax> parameters, string parameterObjectName)
        {
            List<ParameterSyntax> newParameters = new List<ParameterSyntax>(); //method.ParameterList.Parameters.Where(x => !parameters.Contains(x)).ToList();
            newParameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterObjectName.ToLowerFirst())).WithType(SyntaxFactory.IdentifierName(parameterObjectName)));
            MethodDeclarationSyntax newMethod = method;
            if (newParameters.Count == 1)
            {
                newMethod = method.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(newParameters.First())));
            }
            else
            {
                newMethod = method.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(newParameters.ToArray())));
            }

            return newMethod;
        }
        private MethodDeclarationSyntax UpdateMethodBody(MethodDeclarationSyntax method, IEnumerable<ParameterSyntax> parameters, string parameterObjectName, SemanticModel semanticModel)
        {
            var identifiers = method.Body.DescendantTokens()
                .OfType<SyntaxToken>()
                .Where(x => x.Kind() == SyntaxKind.IdentifierToken)
                .Where(x => parameters.Any(y => y.Identifier.ValueText == x.ValueText));
           
            var parameterIdentiferies = new List<SyntaxToken>(identifiers.Count());
            foreach (var token in identifiers)
            {
                var node = method.FindNode(token.Span);
                var symbol = semanticModel.GetSymbolInfo(node).Symbol;
                if (symbol is IParameterSymbol)
                {
                    parameterIdentiferies.Add(token);
                }
            }

            method = method.WithBody(method.Body.ReplaceTokens(parameterIdentiferies, (x, _) => AddPrefixToIdentifier(x, parameterObjectName.ToLowerFirst())));
            return method;
        }
        private SyntaxToken AddPrefixToIdentifier(SyntaxToken originalIdentifier, string prefix)
        {            
            return SyntaxFactory.Identifier($"{prefix}.{originalIdentifier.ValueText.ToUpperFirst()}");
        }

        private async Task<Solution> ReplaceNodes(Solution solution, List<SyntaxNodeToReplace> nodesToReplace, CancellationToken cancellationToken)
        {
            foreach (var item in nodesToReplace)
            {
                item.DocumentId = solution.GetDocumentId(item.OldNode.SyntaxTree);
            }
            var lookup = nodesToReplace.ToDictionary((x) => x.OldNode, (x) => x.NewNode);
            foreach (var group in nodesToReplace.GroupBy(x => x.DocumentId))
            {                
                var document = solution.GetDocument(group.Key);
                var rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false); 
                var oldNodesInDocument = group.Select(x => x.OldNode);               
                rootNode = rootNode.ReplaceNodes(oldNodesInDocument, (x, _) => lookup[x]);  
                document = document.WithSyntaxRoot(rootNode);
                solution = document.Project.Solution;
            }
            return solution;
        }

        private class SyntaxNodeToReplace
        {
            public SyntaxNode OldNode { get; }
            public SyntaxNode NewNode { get; }
            public DocumentId DocumentId {get; set; }

            public SyntaxNodeToReplace(SyntaxNode oldNode, SyntaxNode newNode)
            {
                OldNode = oldNode;
                NewNode = newNode;
            }    
        }
    }
}