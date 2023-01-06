using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenerateMapping.Model
{
    internal class MappingSyntaxGenerator
    {
        public static CSharpSyntaxNode GenerateSyntax(CSharpSyntaxNode syntaxNode, IEnumerable<Match> matches, Accessor firstOutputAccessor)
        {
            var assigmentExpressions = GenerateAssigmentExpressions(matches);

            switch(syntaxNode)
            {
                case ObjectCreationExpressionSyntax objectCreation:
                    var initializerSyntax = SyntaxFactoryEx.ObjectInitializerExpression(assigmentExpressions.Select(x => x.Assignment));
                    var updatedObjectCreation = objectCreation.WithInitializer(initializerSyntax);
                    return updatedObjectCreation;

                case BaseMethodDeclarationSyntax baseMethod:
                    BlockSyntax body = GenerateWrapingCode(assigmentExpressions, firstOutputAccessor);
                    var updatedMethod = baseMethod.WithBody(body).WithTrailingTrivia(baseMethod.Body.GetTrailingTrivia());
                    return updatedMethod;                   
            }

            throw new NotImplementedException();
        }

        private static BlockSyntax GenerateWrapingCode(List<GeneratedExpressionForMatch> assigmentExpressions, Accessor firstOutputAccessor)
        {  
            if (firstOutputAccessor.Name == Accessor.SpecialNameThis)
            {
                var assigmentStatements = assigmentExpressions.Select(x => SyntaxFactory.ExpressionStatement(x.Assignment));
                return SyntaxFactory.Block(assigmentStatements);               
            }

            if (firstOutputAccessor.Type.IsTouple)
            {
                var touple = GenerateToupleExpression(firstOutputAccessor.Type, assigmentExpressions);
                return SyntaxFactory.Block(SyntaxFactory.ReturnStatement(touple));
            }
            
            var statements = new List<StatementSyntax>();

            // {}
            var initializerSyntax = SyntaxFactoryEx.ObjectInitializerExpression(assigmentExpressions.Select(x => x.Assignment));

            // = new accessor.Type.Name() initializerSyntax
            var objectCreationSyntax = SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(firstOutputAccessor.Type.Name), SyntaxFactory.ArgumentList().WithTrailingTrivia(SyntaxFactory.LineFeed), initializerSyntax));

            // var result objectCreationSyntax
            var resultSyntax = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"), SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator("result").WithInitializer(objectCreationSyntax))));
            statements.Add(resultSyntax);

            //  return resultSyntax;
            var returnResultSyntaxt = SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("result"));
            statements.Add(returnResultSyntaxt);

            BlockSyntax body = SyntaxFactory.Block(statements);
            return body;
        }

        private static ExpressionSyntax GenerateToupleExpression(TypeData type, List<GeneratedExpressionForMatch> assigmentExpressions)
        {
            var arguments = new List<ArgumentSyntax>();

            foreach (var name in type.TupleElementNames)
            {
                var generatedExpression = assigmentExpressions.Where(x => x.Match.LeftAccessor.Name == name).FirstOrDefault();
                if (generatedExpression != null)
                {
                    arguments.Add(SyntaxFactory.Argument(generatedExpression.Assignment.Right));
                }
                else
                {
                    arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("default")));
                }
            }            
    
            return SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(arguments));
            //SyntaxFactory.ParseExpression(@"(""d"", 7)");
        }

        private static List<GeneratedExpressionForMatch> GenerateAssigmentExpressions(IEnumerable<Match> matches)
        {
            var assigmentExpressions = new List<GeneratedExpressionForMatch>();

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


                assigmentExpressions.Add(new GeneratedExpressionForMatch(match, SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, right)));
            }

            return assigmentExpressions;
        }

        private static ExpressionSyntax GenerateExpression(Accessor accessor)
        {
            if (accessor.Parent != null && accessor.Parent.Name != Accessor.SpecialNameReturnType)
            {
                return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(accessor.Parent.Name), SyntaxFactory.IdentifierName(accessor.Name));
            }
            else
            {
                return SyntaxFactory.IdentifierName(accessor.Name);
            }
        }

        private class GeneratedExpressionForMatch
        {
            public Match Match { get; }
            public AssignmentExpressionSyntax Assignment { get; }

            public GeneratedExpressionForMatch(Match match, AssignmentExpressionSyntax assignment)
            {
                Match = match;
                Assignment = assignment;
            }
        }       
    }
}