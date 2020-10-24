using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace IntroduceParameterObject
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(IntroduceParameterObjectCodeRefactoringProvider)), Shared]
    internal partial class IntroduceParameterObjectCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var parameters = rootNode.ExtractSelectedNodesOfType<ParameterSyntax>(context.Span);
            var methodDeclarations = rootNode.ExtractSelectedNodesOfType<MethodDeclarationSyntax>(context.Span);

            if (parameters.Any() == false)
            {
                if (methodDeclarations.Any() == false)
                {
                    return;
                }
                parameters = methodDeclarations.First().ParameterList.Parameters;
            }

            var action = CodeAction.Create("Introduce parameter object", c => IntroduceParameterObject(context.Document, methodDeclarations.First(), parameters, c));
            context.RegisterRefactoring(action);
        }

        private async Task<Solution> IntroduceParameterObject(Document document, MethodDeclarationSyntax method, IEnumerable<ParameterSyntax> parameters, CancellationToken cancellationToken)
        {
            IEnumerable<string> folders = null;
            if (document.Folders != null)
            {
                folders = new[] { document.Folders.First() };
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            IMethodSymbol methodSymbol = semanticModel.GetDeclaredSymbol(method, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var parameterObject = new ParameterObjectGenerator.ParameterObject(methodSymbol, parameters, semanticModel);
            var parameterObjectSyntax = ParameterObjectGenerator.CreateParameterObjectClass(parameterObject, parameters);

            cancellationToken.ThrowIfCancellationRequested();

            var nodesToReplace = new List<SyntaxtNodeToReplace>();

            IEnumerable<SymbolCallerInfo> callers = await SymbolFinder.FindCallersAsync(methodSymbol, document.Project.Solution).ConfigureAwait(false);
            foreach (var caller in callers)
            {
                foreach (var location in caller.Locations)
                {
                    var node = location.SourceTree.GetRoot().ExtractSelectedNodesOfType<InvocationExpressionSyntax>(location.SourceSpan).FirstOrDefault();
                    if (node != null)
                    {
                        var constructorInvocation = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(parameterObject.Name)).WithArgumentList(node.ArgumentList);
                        var updateArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(constructorInvocation)));
                        var newNode = node.WithArgumentList(updateArgumentList);
                        nodesToReplace.Add(new SyntaxtNodeToReplace(node, newNode));
                    }
                }
            }

            MethodDeclarationSyntax updatedMethod = UpdateMethodParameterList(method, parameters, parameterObject);
            updatedMethod = UpdateMethodBody(parameters, parameterObject, updatedMethod);
            nodesToReplace.Add(new SyntaxtNodeToReplace(method, updatedMethod));

            Solution solution = await Replace(document.Project.Solution, nodesToReplace, cancellationToken).ConfigureAwait(false);
            //document = await ReplaceMethodInDocument(document, method, updatedMethod, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var project = solution.GetProject(document.Project.Id);
            var parameterObjectDocument = project.AddDocument(parameterObject.Name + ".cs", parameterObjectSyntax, folders);

            cancellationToken.ThrowIfCancellationRequested();

            return parameterObjectDocument.Project.Solution;
        }

        private async Task<Solution> Replace(Solution solution, List<SyntaxtNodeToReplace> nodesToReplace, CancellationToken cancellationToken)
        {
            foreach (var toReplace in nodesToReplace)
            {
                toReplace.DocumentId = solution.GetDocumentId(toReplace.OldNode.SyntaxTree);
            }
            foreach (var group in nodesToReplace.GroupBy(x => x.DocumentId))
            {                
                var document = solution.GetDocument(group.Key);
                var rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                foreach (var item in group)
                {
                    var oldNodesInDocument = group.Select(x => x.OldNode);
                    rootNode = rootNode.ReplaceNodes(oldNodesInDocument, (x, _) =>  group.Where(y => y.OldNode == x).Select(z => z.NewNode).Single());                   
                }
                document = document.WithSyntaxRoot(rootNode);
                solution = document.Project.Solution;
            }
            return solution;
        }

        private MethodDeclarationSyntax UpdateMethodBody(IEnumerable<ParameterSyntax> parameters, ParameterObjectGenerator.ParameterObject parameterObject, MethodDeclarationSyntax newMethod)
        {
            var identifiers = newMethod.Body.DescendantTokens().OfType<SyntaxToken>().Where(x => x.Kind() == SyntaxKind.IdentifierToken);
            newMethod = newMethod.WithBody(newMethod.Body.ReplaceTokens(identifiers, (x, _) => AddPrefixToIdentifier(x, parameters, parameterObject.Name.ToLowerFirst())));
            return newMethod;
        }

        private SyntaxToken AddPrefixToIdentifier(SyntaxToken originalIdentifier, IEnumerable<ParameterSyntax> parameters, string prefix)
        {
            if (!parameters.Any(x => x.Identifier.ValueText == originalIdentifier.ValueText))
            {
                return originalIdentifier;
            }
            return SyntaxFactory.Identifier($"{prefix}.{originalIdentifier.ValueText.ToUpperFirst()}");
        }

        private static async Task<Document> ReplaceMethodInDocument(Document document, MethodDeclarationSyntax method, MethodDeclarationSyntax newMethod, CancellationToken cancellationToken)
        {
            var rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = rootNode.ReplaceNode(method, newMethod);
            document = document.WithSyntaxRoot(newRoot);
            return document;
        }

        private static MethodDeclarationSyntax UpdateMethodParameterList(MethodDeclarationSyntax method, IEnumerable<ParameterSyntax> parameters, ParameterObjectGenerator.ParameterObject parameterObject)
        {
            List<ParameterSyntax> newParameters = method.ParameterList.Parameters.Where(x => !parameters.Contains(x)).ToList();
            newParameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterObject.Name.ToLowerFirst())).WithType(SyntaxFactory.IdentifierName(parameterObject.Name)));
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


        private class SyntaxtNodeToReplace
        {
            public SyntaxNode OldNode { get; }
            public SyntaxNode NewNode { get; }
            public DocumentId DocumentId {get; set; }

            public SyntaxtNodeToReplace(SyntaxNode oldNode, SyntaxNode newNode)
            {
                OldNode = oldNode;
                NewNode = newNode;
            }    
        }
    }
}
