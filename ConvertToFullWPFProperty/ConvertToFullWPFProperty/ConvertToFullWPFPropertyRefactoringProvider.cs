﻿using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace ConvertToFullWPFProperty
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
                var action = CodeAction.Create("Convert to full WPF property", cancellationToken => ConvertToFullWPFProperty(context.Document, selectedAutoPropertyDeclarationSyntaxes, cancellationToken));
                context.RegisterRefactoring(action);
            }
        }
      

        private async Task<Document> ConvertToFullWPFProperty(Document document, IEnumerable<PropertyDeclarationSyntax> selectedAutoPropertyDeclarationSyntaxes, CancellationToken cancellationToken)
        {
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var containingTypeNode = selectedAutoPropertyDeclarationSyntaxes.First().Parent as TypeDeclarationSyntax;
            INamedTypeSymbol containingTypeSymbol = semanticModel.GetDeclaredSymbol(containingTypeNode);

            cancellationToken.ThrowIfCancellationRequested();

            string methodNameToNotifyThatPropertyWasChanged = await containingTypeSymbol.DetermineMethodNameUsedToNotifyThatPropertyWasChanged(document.Project.Solution).ConfigureAwait(false);
            char? backingFiledPrefix = containingTypeSymbol.DetermineBackingFiledPrefix();

            cancellationToken.ThrowIfCancellationRequested();
            
            SyntaxNode newTypeNode = containingTypeNode.ReplaceNodes(selectedAutoPropertyDeclarationSyntaxes, (x, _) => CreateFullProperty(x, backingFiledPrefix, methodNameToNotifyThatPropertyWasChanged, syntaxGenerator));            

            cancellationToken.ThrowIfCancellationRequested();

            List<SyntaxNode> createdBackingFields = CreateBackingFields(selectedAutoPropertyDeclarationSyntaxes, backingFiledPrefix, syntaxGenerator);
            newTypeNode = InsertCreatedBackingFields(newTypeNode, createdBackingFields);

            cancellationToken.ThrowIfCancellationRequested();
            
            Document newDocument = await CreateNewDocument(document, containingTypeNode, newTypeNode, cancellationToken).ConfigureAwait(false);
            return newDocument;
        }

        private SyntaxNode CreateFullProperty(PropertyDeclarationSyntax property, char? backingFiledPrefix, string methodNameToNotifyThatPropertyWasChanged, SyntaxGenerator syntaxGenerator)
        {
            string propertyName = property.Identifier.ValueText;
            string fieldName = FieldNameGenerator.Generate(propertyName, backingFiledPrefix);

            var leadingTrivia = property.GetLeadingTrivia();
            var trailingTrivia = property.GetTrailingTrivia();

            return syntaxGenerator.FullPropertyDeclaration(propertyName, property.Type, property.AttributeLists, leadingTrivia, trailingTrivia, fieldName, methodNameToNotifyThatPropertyWasChanged);
        }
        private List<SyntaxNode> CreateBackingFields(IEnumerable<PropertyDeclarationSyntax> properties, char? backingFiledPrefix, SyntaxGenerator syntaxGenerator)
        {
            var createdBackingFields = new List<SyntaxNode>();

            foreach (PropertyDeclarationSyntax property in properties)
            {
                string propertyName = property.Identifier.ValueText;
                string fieldName = FieldNameGenerator.Generate(propertyName, backingFiledPrefix);
                var createdField = syntaxGenerator.FieldDeclaration(fieldName, property.Type, Accessibility.Private);
                createdBackingFields.Add(createdField);
            }

            return createdBackingFields;
        }
        private SyntaxNode InsertCreatedBackingFields(SyntaxNode typeNode, IEnumerable<SyntaxNode> backingFields)
        {
            SyntaxNode result = typeNode;

            MemberDeclarationSyntax insertAfterThisNode = typeNode.DescendantNodes().OfType<FieldDeclarationSyntax>().LastOrDefault();            

            if (insertAfterThisNode != null)
            {
                result = result.InsertNodesAfter(insertAfterThisNode, backingFields);
            }
            else
            {
                MemberDeclarationSyntax insertBeforeThisNode = typeNode.DescendantNodes().OfType<PropertyDeclarationSyntax>().First();
                result = result.InsertNodesBefore(insertBeforeThisNode, backingFields);
            }

            return result;
        }
        private async Task<Document> CreateNewDocument(Document document, SyntaxNode oldTypeNode, SyntaxNode newTypeNode, CancellationToken cancellationToken)
        {
            SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            rootNode = rootNode.ReplaceNode(oldTypeNode, newTypeNode);
            return document.WithSyntaxRoot(rootNode);
        }
    }
}