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
            var selectedAutoPropertyDeclarationSyntaxes = rootNode.ExtractSelectedNodesOfType<PropertyDeclarationSyntax>(context.Span).Where(x => x.IsAutoProperty());

            if (selectedAutoPropertyDeclarationSyntaxes.Any())
            {
                var action = CodeAction.Create("Convert to full WPF ppoperty", cancellationToken => ConvertToFullWPFProperty(context.Document, selectedAutoPropertyDeclarationSyntaxes, cancellationToken));
                context.RegisterRefactoring(action);
            }
        }
      

        private async Task<Document> ConvertToFullWPFProperty(Document document, IEnumerable<PropertyDeclarationSyntax> selectedAutoPropertyDeclarationSyntaxes, CancellationToken cancellationToken)
        {
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);

            var typeNode = selectedAutoPropertyDeclarationSyntaxes.First().Parent as TypeDeclarationSyntax;
            INamedTypeSymbol typeSymbol = semanticModel.GetDeclaredSymbol(typeNode);

            string methodNameToNotifyThatPropertyWasChanged = await typeSymbol.DetermineMethodNameUsedToNotifyThatPropertyWasChanged(document.Project.Solution).ConfigureAwait(false);
            
            List<SyntaxNode> createdBackingFields = new List<SyntaxNode>();
            SyntaxNode newTypeNode = typeNode.ReplaceNodes(selectedAutoPropertyDeclarationSyntaxes, CreateFullProperty);

            SyntaxNode CreateFullProperty(PropertyDeclarationSyntax property, PropertyDeclarationSyntax _)
            {
                string propertyName = property.Identifier.ValueText;
                string fieldName = FieldNameGenerator.Generate(propertyName, '_');
                var createdField = syntaxGenerator.FieldDeclaration(fieldName, property.Type, Accessibility.Private);
                createdBackingFields.Add(createdField);

                return syntaxGenerator.FullPropertyDeclaration(propertyName, property.Type, fieldName, methodNameToNotifyThatPropertyWasChanged);
            }

            newTypeNode = InsertCreatedBackingFields(newTypeNode, createdBackingFields);

            SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            rootNode = rootNode.ReplaceNode(typeNode, newTypeNode);
            return document.WithSyntaxRoot(rootNode);
        }


        private SyntaxNode InsertCreatedBackingFields(SyntaxNode typeNode, List<SyntaxNode> createdBackingFields)
        {
            SyntaxNode result = typeNode;

            MemberDeclarationSyntax insertAfterThisNode = result.DescendantNodes().OfType<FieldDeclarationSyntax>().Last();
            if (insertAfterThisNode != null)
            {
                result = result.InsertNodesAfter(insertAfterThisNode, createdBackingFields);
            }
            else
            {
                MemberDeclarationSyntax insertBeforeThisNode = typeNode.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
                result = result.InsertNodesBefore(insertBeforeThisNode, createdBackingFields);
            }

            return result;
        }
    }
}
