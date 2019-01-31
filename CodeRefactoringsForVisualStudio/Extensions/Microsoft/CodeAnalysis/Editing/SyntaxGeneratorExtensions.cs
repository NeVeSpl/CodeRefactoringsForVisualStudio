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
                //SyntaxFactory.ParseStatement($"OnPropertyChanged();")
                //SyntaxFactory.ParseStatement($"{fieldName} = value;").WithTrailingTrivia(new[] { SyntaxFactory.ElasticCarriageReturn }),
                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(fieldName), SyntaxFactory.IdentifierName("value") )),//.WithTrailingTrivia(new[] { SyntaxFactory.ElasticCarriageReturn }),
                SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(methodNameToNotifyThatPropertyWasChanged)))
            };

            var createdProperty = syntaxGenerator.PropertyDeclaration(propertyName,
                                                                         propertyType,
                                                                         Accessibility.Public,
                                                                         getAccessorStatements: getAccessorStatements,
                                                                         setAccessorStatements: setAccessorStatements) as PropertyDeclarationSyntax;
            //createdProperty.AccessorList.wit

            return createdProperty;
        }
    }
}
