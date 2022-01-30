using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenerateMapping.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenerateMapping
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(GenerateMappingCodeRefactoringProvider)), Shared]
    public class GenerateMappingCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {   
            var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var selecteObjectCreation = rootNode.ExtractSelectedNodesOfType<ObjectCreationExpressionSyntax>(context.Span).OfType<CSharpSyntaxNode>().FirstOrDefault();
            var selectedMethodDeclaration = rootNode.ExtractSelectedNodesOfType<MethodDeclarationSyntax>(context.Span, false).OfType<CSharpSyntaxNode>().FirstOrDefault();
            var selectedConstructorDeclaration = rootNode.ExtractSelectedNodesOfType<ConstructorDeclarationSyntax>(context.Span, false).OfType<CSharpSyntaxNode>().FirstOrDefault();

            var selectedNode = selecteObjectCreation ?? selectedMethodDeclaration ?? selectedConstructorDeclaration;

            if (selectedNode != null)
            {
                var action = CodeAction.Create("Generate mapping", c => GenerateMapping(context.Document, selectedNode, c));
                context.RegisterRefactoring(action);
            }
        }

        private async Task<Document> GenerateMapping(Document document, CSharpSyntaxNode node, CancellationToken cancellationToken)
        {
            (var leftAccessors, var rightAccessors) = await AccessorsExtractor.GetAccessors(document, node, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested(); 

            var matches = AccessorsMatcher.DoMatching(leftAccessors, rightAccessors);
            cancellationToken.ThrowIfCancellationRequested();

            // todo : Load meatdata

            var updatedNode = MappingSyntaxGenerator.GenerateSyntax(node, matches, leftAccessors.First());
            cancellationToken.ThrowIfCancellationRequested();

            SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            rootNode = rootNode.ReplaceNode(node, updatedNode);
            return document.WithSyntaxRoot(rootNode);
        }
    }
}