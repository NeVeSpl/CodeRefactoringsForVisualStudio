using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Editing
{
    public static class SyntaxGeneratorExtensions
    {
        public static SyntaxNode FullPropertyDeclaration(this SyntaxGenerator syntaxGenerator, string propertyName, SyntaxNode propertyType, string fieldName, string methodNameToNotifyThatPropertyWasChanged)
        {
            var getAccessorStatements = new List<StatementSyntax>()
            {
                //SyntaxFactory.ParseStatement($"return {fieldName};")
                SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(fieldName))
            };

            var setAccessorStatements = new List<StatementSyntax>()
            {
                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(fieldName), SyntaxFactory.IdentifierName("value") )),
                SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(methodNameToNotifyThatPropertyWasChanged)))
            };

            //var createdProperty = syntaxGenerator.PropertyDeclaration(propertyName,
            //                                                             propertyType,
            //                                                             Accessibility.Public,
            //                                                             getAccessorStatements: getAccessorStatements,
            //                                                             setAccessorStatements: setAccessorStatements) as PropertyDeclarationSyntax;
            var accessors = new List<AccessorDeclarationSyntax>();

            accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, SyntaxFactory.Block(getAccessorStatements)).WithoutTrivia());
            accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, SyntaxFactory.Block(setAccessorStatements)).WithoutTrivia());

            var createdProperty = SyntaxFactory.PropertyDeclaration(
                default,
                SyntaxFactory.TokenList(new SyntaxToken[]{ SyntaxFactory.Token(SyntaxKind.PublicKeyword)}),
                (TypeSyntax)propertyType,
                default,
                SyntaxFactory.Identifier(propertyName),
                SyntaxFactory.AccessorList(SyntaxFactory.List(accessors))).WithoutTrivia();

            return createdProperty;
        }
    }
}
