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

            var createdProperty = syntaxGenerator.PropertyDeclaration(propertyName,
                                                                         propertyType,
                                                                         Accessibility.Public,
                                                                         getAccessorStatements: getAccessorStatements,
                                                                         setAccessorStatements: setAccessorStatements) as PropertyDeclarationSyntax;            

            return createdProperty;
        }
    }
}
