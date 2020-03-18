using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;


namespace EncapsulateFieldForWPF
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(EncapsulateFieldForWPFRefactoringProvider)), Shared]
    public class EncapsulateFieldForWPFRefactoringProvider :CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            SyntaxNode rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var selectedFieldDeclarations = rootNode.ExtractSelectedNodesOfType<FieldDeclarationSyntax>(context.Span);

            if (selectedFieldDeclarations.Any())
            {
                var action = CodeAction.Create("Encapsulate field (WPF)", cancellationToken => EncapsulateFields(context.Document, selectedFieldDeclarations, cancellationToken));
                context.RegisterRefactoring(action);
            }
        }


        private async Task<Document> EncapsulateFields(Document document, IEnumerable<FieldDeclarationSyntax> fieldDeclarations, CancellationToken cancellationToken)
        {
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document.Project);         
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);       
            var typeNode = fieldDeclarations.First().Parent as TypeDeclarationSyntax;     
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeNode);
          
            cancellationToken.ThrowIfCancellationRequested();
          
            string methodNameToNotifyThatPropertyWasChanged = await typeSymbol.DetermineMethodNameUsedToNotifyThatPropertyWasChanged(document.Project.Solution).ConfigureAwait(false);
       
            cancellationToken.ThrowIfCancellationRequested();
           
            List<SyntaxNode> createdProperties = CreateProperties(fieldDeclarations, syntaxGenerator, methodNameToNotifyThatPropertyWasChanged);
         
            cancellationToken.ThrowIfCancellationRequested();
           
            SyntaxNode insertAfterThisNode = FindNodeAfterWhichCreatedPropertiesWillBeInserted(fieldDeclarations);
           
            cancellationToken.ThrowIfCancellationRequested();
           
            Document newDocument = await CreateNewDocument(document, typeNode, createdProperties, insertAfterThisNode, cancellationToken).ConfigureAwait(false);
       
            return newDocument;
        }
      
        private List<SyntaxNode> CreateProperties(IEnumerable<FieldDeclarationSyntax> fieldDeclarations, SyntaxGenerator syntaxGenerator, string methodNameToNotifyThatPropertyWasChanged)
        {
            var createdProperties = new List<SyntaxNode>();

            foreach (FieldDeclarationSyntax fieldDeclaration in fieldDeclarations)
            {
                foreach (VariableDeclaratorSyntax variableDeclarator in fieldDeclaration.Declaration.Variables)
                {
                    string fieldName = variableDeclarator.Identifier.ValueText;
                    string propertyName = PropertyNameGenerator.FromFieldName(fieldName);
                   
                    SyntaxNode newProperty = syntaxGenerator.FullPropertyDeclaration(propertyName, fieldDeclaration.Declaration.Type, fieldName, methodNameToNotifyThatPropertyWasChanged);
                    createdProperties.Add(newProperty);
                }
            }

            return createdProperties;
        }
        private SyntaxNode FindNodeAfterWhichCreatedPropertiesWillBeInserted(IEnumerable<FieldDeclarationSyntax> fieldDeclarations)
        {
            var typeNode = fieldDeclarations.First().Parent as TypeDeclarationSyntax; 

            MemberDeclarationSyntax lastMember = fieldDeclarations.Last();

            foreach (MemberDeclarationSyntax member in typeNode.Members)
            {
                if (member is PropertyDeclarationSyntax)
                {
                    lastMember = member;
                }
            }

            // TODO : Right now the newly created property is added after the last property in the containing type,
            //        though it would be nice to insert it in a place that will preserve the same order for fields and properties 

            return lastMember;
        }
        private async Task<Document> CreateNewDocument(Document document, TypeDeclarationSyntax typeNode, IEnumerable<SyntaxNode> createdProperties, SyntaxNode insertAfterThisNode, CancellationToken cancellationToken)
        {
            TypeDeclarationSyntax newTypeNode = typeNode.InsertNodesAfter(insertAfterThisNode, createdProperties);
            SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            rootNode = rootNode.ReplaceNode(typeNode, newTypeNode);
            return document.WithSyntaxRoot(rootNode);
        }

    }
}
