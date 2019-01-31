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

namespace CodeRefactoringsForVisualStudio.Refactorings.ConvertToFullWPFProperty
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ConvertToFullWPFPropertyRefactoringProvider)), Shared]
    public class ConvertToFullWPFPropertyRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            SyntaxNode rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var selectedAutoPropertyDeclarationSyntaxes = rootNode.ExtractSelectedNodesOfType<PropertyDeclarationSyntax>(context.Span).Where(x => IsAutoProperty(x));

            if (selectedAutoPropertyDeclarationSyntaxes.Any())
            {
                var action = CodeAction.Create("Convert to full WPF ppoperty", cancellationToken => ConvertToFullWPFProperty(context.Document, selectedAutoPropertyDeclarationSyntaxes, cancellationToken));
                context.RegisterRefactoring(action);
            }
        }

        private bool IsAutoProperty(PropertyDeclarationSyntax property)
        {
            return property.AccessorList.Accessors.All(x => x.Body == null);
        }

        private async Task<Document> ConvertToFullWPFProperty(Document document, IEnumerable<PropertyDeclarationSyntax> selectedAutoPropertyDeclarationSyntaxes, CancellationToken cancellationToken)
        {
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);          
            var typeNode = selectedAutoPropertyDeclarationSyntaxes.First().Parent as TypeDeclarationSyntax;

            List<SyntaxNode> createdBackingFields = new List<SyntaxNode>();
            SyntaxNode newTypeNode = typeNode.ReplaceNodes(selectedAutoPropertyDeclarationSyntaxes, CreateFullProperty);

            SyntaxNode CreateFullProperty(PropertyDeclarationSyntax property, PropertyDeclarationSyntax _)
            {
                string propertyName = property.Identifier.ValueText;
                string fieldName = "_" + propertyName;
                var createdField = syntaxGenerator.FieldDeclaration(fieldName, property.Type, Accessibility.Private);
                createdBackingFields.Add(createdField);

                return syntaxGenerator.FullPropertyDeclaration(propertyName, property.Type, fieldName, "OnPropertyChanged");
            }

            MemberDeclarationSyntax insertAfterThisNode = newTypeNode.DescendantNodes().OfType<FieldDeclarationSyntax>().Last();
            if (insertAfterThisNode != null)
            {
                newTypeNode = newTypeNode.InsertNodesAfter(insertAfterThisNode, createdBackingFields);
            }
            else
            {
                MemberDeclarationSyntax insertBeforeThisNode = typeNode.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
                newTypeNode = newTypeNode.InsertNodesBefore(insertBeforeThisNode, createdBackingFields);
            }

            SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            rootNode = rootNode.ReplaceNode(typeNode, newTypeNode);
            return document.WithSyntaxRoot(rootNode);
        }
    }
}
