using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using GenerateMapping.Model;

namespace GenerateMapping
{
    internal class MappingSyntaxGenerator
    {
        public static BaseMethodDeclarationSyntax GenerateSyntax(BaseMethodDeclarationSyntax methodDeclaration, IEnumerable<Match> matches, Accessor accessor)
        {
            List<ExpressionSyntax> assigmentExpressions = GenerateAssigmentExpressions(matches);
            BlockSyntax body = GenerateWrapingCode(assigmentExpressions, accessor);           

            var updatedMethod = methodDeclaration.WithBody(body).WithTrailingTrivia(methodDeclaration.Body.GetTrailingTrivia());
            return updatedMethod;
        }

        private static BlockSyntax GenerateWrapingCode(List<ExpressionSyntax> assigmentExpressions, Accessor accessor)
        {
            BlockSyntax body;

            if (accessor.Name == "this")
            {
                var assigmentStatements = assigmentExpressions.Select(x => SyntaxFactory.ExpressionStatement(x));
                body = SyntaxFactory.Block(assigmentStatements);
            }
            else
            {
                var statements = new List<StatementSyntax>();

                // {}
                var initializer = SyntaxFactory.InitializerExpression(SyntaxKind.CollectionInitializerExpression, SyntaxFactory.SeparatedList<ExpressionSyntax>(assigmentExpressions));

                // = new accessor.Type.Name() initializer
                var initSyntaxt = SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(accessor.Type.Name), SyntaxFactory.ArgumentList().WithTrailingTrivia(SyntaxFactory.LineFeed), initializer));

                // var result initSyntaxt
                var resultStatement = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"), SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator("result").WithInitializer(initSyntaxt))));
                statements.Add(resultStatement);

                //  return result;
                var returnResultSyntaxt = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));
                statements.Add(returnResultSyntaxt);

                body = SyntaxFactory.Block(statements);
            }

            return body;
        }

        private static List<ExpressionSyntax> GenerateAssigmentExpressions(IEnumerable<Match> matches)
        {
            var assigmentExpressions = new List<ExpressionSyntax>();

            foreach (var match in matches)
            {
                TypeData leftType = match.LeftAccessor.Type;
                var left = GenerateExpression(match.LeftAccessor);
                var right = GenerateExpression(match.RightAccessor);
                
                if (leftType.IsCollection && leftType.Arguments.Count() == 1)
                {                   
                    var arg = leftType.Arguments.First();
                    if (!arg.IsImmutable)
                    {
                        // x => new arg.Name(x)
                        var lambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactoryEx.Parameter("x"), SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(arg.Name), SyntaxFactoryEx.ArgumentListWithOneArgument(SyntaxFactory.IdentifierName("x")), null));

                        // rightExpression.Select(lambda)
                        right = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, right, SyntaxFactory.IdentifierName("Select")), SyntaxFactoryEx.ArgumentListWithOneArgument(lambda));
                    }
                    
                }

                if (!match.LeftAccessor.Type.IsImmutable)
                {
                    if (match.LeftAccessor.Type.IsCollection && match.LeftAccessor.Type.IsInterface)
                    {
                        // rightExpression.ToList()
                        right = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, right, SyntaxFactory.IdentifierName("ToList")));
                    }
                    else
                    {
                        if (match.LeftAccessor.Type.IsArray)
                        {
                            // rightExpression.ToArray()
                            right = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, right, SyntaxFactory.IdentifierName("ToArray")));
                        }
                        else
                        {
                            // new LeftReferenceType(rightExpression)
                            right = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(match.LeftAccessor.Type.Name), SyntaxFactoryEx.ArgumentListWithOneArgument(right), null);
                        }
                    }
                }


                assigmentExpressions.Add(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right));
            }

            return assigmentExpressions;
        }

        private static ExpressionSyntax GenerateExpression(Accessor accessor)
        {
            if (accessor.Parent != null && accessor.Parent.Name != "result")
            {
                return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(accessor.Parent.Name), SyntaxFactory.IdentifierName(accessor.Name));
            }
            else
            {
                return SyntaxFactory.IdentifierName(accessor.Name);
            }
        }
    }
}