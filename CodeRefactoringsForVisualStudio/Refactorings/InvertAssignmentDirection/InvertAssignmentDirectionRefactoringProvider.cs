using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace CodeRefactoringsForVisualStudio.Refactorings.InvertAssignmentDirection
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(InvertAssignmentDirectionRefactoringProvider)), Shared]
    public class InvertAssignmentDirectionRefactoringProvider :CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            SyntaxNode rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var selectedAssignmentExpressions = rootNode.ExtractSelectedNodesOfType<AssignmentExpressionSyntax>(context.Span).Where(x => IsHandledExpressionSyntax(x.Left) && IsHandledExpressionSyntax(x.Right));

            if (selectedAssignmentExpressions.Any())
            {
                var action = CodeAction.Create("Invert assignment", cancellationToken => InvertAssignments(context.Document, selectedAssignmentExpressions, cancellationToken));
                context.RegisterRefactoring(action);
            }
        }


        private async Task<Document> InvertAssignments(Document document, IEnumerable<AssignmentExpressionSyntax> assignmentExpressions, CancellationToken cancellationToken)
        {
            SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            rootNode = rootNode.ReplaceNodes(assignmentExpressions, InvertAssignmentExpression); 
            return document.WithSyntaxRoot(rootNode);
        }

        private SyntaxNode InvertAssignmentExpression(AssignmentExpressionSyntax originalNode, AssignmentExpressionSyntax _)
        {
            ExpressionSyntax left = originalNode.Right.WithoutTrivia().WithTriviaFrom(originalNode.Left);
            ExpressionSyntax right = originalNode.Left.WithoutTrivia();
            SyntaxNode invertedAssigment = originalNode.WithLeft(left).WithRight(right);

            return invertedAssigment;
        }

        private bool IsHandledExpressionSyntax(CSharpSyntaxNode node)
        {
            bool result = false;
            switch (node)
            {
                case IdentifierNameSyntax _:
                case MemberAccessExpressionSyntax _:
                    result = true;
                    break;
            }
            return result;
        }
    }
}
