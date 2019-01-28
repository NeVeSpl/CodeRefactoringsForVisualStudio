using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;


namespace CodeRefactoringsForVisualStudio.Refactorings.EncapsulateFieldForWPF
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
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);

            var typeNode = fieldDeclarations.First().Parent as TypeDeclarationSyntax;
            SemanticModel semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            string methodNameToNotifyThatPropertyWasChanged = await DetermineMethodNameUsedToNotifyThatPropertyWasChanged(semanticModel, typeNode, document.Project.Solution).ConfigureAwait(false);
            List<SyntaxNode> createdProperties = CreateProperties(fieldDeclarations, syntaxGenerator, methodNameToNotifyThatPropertyWasChanged);

            cancellationToken.ThrowIfCancellationRequested();
        
            SyntaxNode insertAfterThisNode = FindNodeAfterWhichCreatedPropertiesWillBeInserted(fieldDeclarations);

            cancellationToken.ThrowIfCancellationRequested();

            TypeDeclarationSyntax newTypeNode = typeNode.InsertNodesAfter(insertAfterThisNode, createdProperties);

            SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            rootNode = rootNode.ReplaceNode(typeNode, newTypeNode);
            return document.WithSyntaxRoot(rootNode);
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

                    var getAccessorStatements = new List<StatementSyntax>()
                    {
                        //SyntaxFactory.ParseStatement($"return {fieldName};")
                        SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(fieldName))
                    };

                    var setAccessorStatements = new List<StatementSyntax>()
                    {
                       //SyntaxFactory.ParseStatement($"OnPropertyChanged();")
                       //SyntaxFactory.ParseStatement($"{fieldName} = value;").WithTrailingTrivia(new[] { SyntaxFactory.ElasticCarriageReturn }),
                       SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(fieldName), SyntaxFactory.IdentifierName("value") )).WithTrailingTrivia(new[] { SyntaxFactory.ElasticCarriageReturn }),
                       SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(methodNameToNotifyThatPropertyWasChanged)))
                    };

                    SyntaxNode newProperty = syntaxGenerator.PropertyDeclaration(propertyName,
                                                                                 fieldDeclaration.Declaration.Type,
                                                                                 Accessibility.Public,
                                                                                 getAccessorStatements: getAccessorStatements,
                                                                                 setAccessorStatements: setAccessorStatements).WithLeadingTrivia(fieldDeclaration.GetLeadingTrivia());

                    createdProperties.Add(newProperty);
                }
            }

            return createdProperties;
        }

        private SyntaxNode FindNodeAfterWhichCreatedPropertiesWillBeInserted(IEnumerable<FieldDeclarationSyntax> fieldDeclarations)
        {
            var classNode = fieldDeclarations.First().Parent as TypeDeclarationSyntax;
          
            //var descendantTokens = fieldDeclarations.SelectMany(x => x.DescendantTokens().OfType<SyntaxToken>().Where(y => y.Kind() == SyntaxKind.IdentifierToken));

            MemberDeclarationSyntax lastMember = fieldDeclarations.Last();

            foreach (MemberDeclarationSyntax member in classNode.Members)
            {
                if (member is PropertyDeclarationSyntax)
                {
                    lastMember = member;
                }
            }

            return lastMember;
        }

        private async Task<string> DetermineMethodNameUsedToNotifyThatPropertyWasChanged(SemanticModel semanticModel, TypeDeclarationSyntax typeNode, Solution solution)
        {
            String result = "OnPropertyChanged";          
           
            INamedTypeSymbol typeSymbol = semanticModel.GetDeclaredSymbol(typeNode);
            IAssemblySymbol assemblySymbol = typeSymbol.ContainingAssembly;

            var typesInInheritanceHierarchy = new HashSet<INamedTypeSymbol>();

            var currentType = typeSymbol;
            while (currentType != null)
            {
                typesInInheritanceHierarchy.Add(currentType);
                currentType = currentType.BaseType;
            }

            foreach (INamedTypeSymbol interfaceSymbol in typeSymbol.AllInterfaces)
            {
                if (interfaceSymbol.Name == "INotifyPropertyChanged" && String.Equals(interfaceSymbol?.ContainingNamespace.ToString(), "System.ComponentModel"))
                {
                    ISymbol propertyChangedEventSymbol = interfaceSymbol.GetMembers("PropertyChanged").First();
                    IEnumerable<SymbolCallerInfo> callers = await SymbolFinder.FindCallersAsync(propertyChangedEventSymbol, solution).ConfigureAwait(false);

                    foreach (SymbolCallerInfo caller in callers)
                    {
                        if (typesInInheritanceHierarchy.Contains(caller.CallingSymbol.ContainingType))
                        {
                            result = caller.CallingSymbol.Name;
                        }
                    }
                }
            }

            return result;
        }
    }
}
