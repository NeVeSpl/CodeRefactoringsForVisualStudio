using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

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
        public static SyntaxNode FullPrismPropertyDeclaration(this SyntaxGenerator syntaxGenerator, string propertyName, SyntaxNode propertyType, string fieldName, string methodNameToNotifyThatPropertyWasChanged)
        {
            var getAccessorStatements = new List<StatementSyntax>()
            {
                //SyntaxFactory.ParseStatement($"return {fieldName};")
                SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(fieldName))
            };
            string prismName = "SetProperty";
            var setAccessorStatements = new List<StatementSyntax>()
            {
             

               // SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(fieldName), SyntaxFactory.IdentifierName("value") )),
                SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(prismName)) .WithArgumentList
                                            (
                                                SyntaxFactory.ArgumentList
                                                (
                                                    SyntaxFactory.SeparatedList<ArgumentSyntax>
                                                    (
                                                        new SyntaxNodeOrToken[]
                                                        {
                                                            SyntaxFactory.Argument
                                                            (
                                                                SyntaxFactory.IdentifierName(fieldName))
                                                            .WithRefKindKeyword
                                                            (
                                                                SyntaxFactory.Token(SyntaxKind.RefKeyword)),
                                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                            SyntaxFactory.Argument
                                                            (
                                                                SyntaxFactory.IdentifierName("value"))}))))
            };

            var accessors = new List<AccessorDeclarationSyntax>();

            accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, SyntaxFactory.Block(getAccessorStatements)).WithoutTrivia());
            accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, SyntaxFactory.Block(setAccessorStatements)).WithoutTrivia());

            var createdProperty = SyntaxFactory.PropertyDeclaration(
                default,
                SyntaxFactory.TokenList(new SyntaxToken[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }),
                (TypeSyntax)propertyType,
                default,
                SyntaxFactory.Identifier(propertyName),
                SyntaxFactory.AccessorList(SyntaxFactory.List(accessors))).WithAdditionalAnnotations(Formatter.Annotation).NormalizeWhitespace();

            return createdProperty.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
               .WithLeadingTrivia(propertyType.GetLeadingTrivia().ToArray())
               .WithAdditionalAnnotations(Simplifier.Annotation);
            //return createdProperty;
        }
    }

}
