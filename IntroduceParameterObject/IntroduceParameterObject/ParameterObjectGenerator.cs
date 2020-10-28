using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntroduceParameterObject
{
    internal class ParameterObjectGenerator
    {
        public static CompilationUnitSyntax CreateParameterObjectClass(ParameterObject parameterObject, IEnumerable<ParameterSyntax> parameters)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit();

            compilationUnit = compilationUnit.AddUsings(parameterObject.Usings);
            var @namespace = SyntaxFactoryEx.NamespaceDeclaration(parameterObject.Namespace);
            var classDeclaration = SyntaxFactoryEx.InternalClassDeclaration(parameterObject.Name);

            var members = new List<MemberDeclarationSyntax>();
            foreach (var property in parameterObject.Properties)
            {
                var propertyDeclaration = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(property.Type), property.PropertyName)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

                members.Add(propertyDeclaration);
            }

            var body = new List<StatementSyntax>();
            foreach (var property in parameterObject.Properties)
            {
                var assignment = SyntaxFactory.ExpressionStatement
                                (
                                    SyntaxFactory.AssignmentExpression
                                    (
                                        SyntaxKind.SimpleAssignmentExpression,
                                        SyntaxFactory.IdentifierName(property.PropertyName),
                                        SyntaxFactory.IdentifierName(property.ParameterName)
                                    )
                                );
                body.Add(assignment);
            }

            var constructor = SyntaxFactory.ConstructorDeclaration(SyntaxFactory.Identifier(parameterObject.Name))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(parameters.ToArray())
            .AddBodyStatements(body.ToArray());

            members.Add(constructor);

            classDeclaration = classDeclaration.AddMembers(members.ToArray());
            @namespace = @namespace.AddMembers(classDeclaration);
            compilationUnit = compilationUnit.AddMembers(@namespace);
            var code = compilationUnit.NormalizeWhitespace();

            return code;
        }

        public class ParameterObject
        {
            public List<string> Usings { get;  }
            public string Name { get;  }
            public string Namespace { get;  }
            public List<(string PropertyName, string ParameterName, string Type)> Properties { get; }


            public ParameterObject(IMethodSymbol method, IEnumerable<ParameterSyntax> parameters, SemanticModel semanticModel)
            {
                Usings = new List<string> { "System" };
                Name = method.Name + "ParameterObject";
                Namespace = method.ContainingNamespace.ToString();
                Properties = new List<(string, string, string)>();

                foreach (var parameter in parameters)
                {
                    var parameterName = parameter.Identifier.ValueText;
                    var propertyName = parameterName.ToUpperFirst();
                    var type = parameter.Type;
                    var typeInfo = semanticModel.GetTypeInfo(type);                   
                    Usings.AddRange(typeInfo.Type.GetUsings());                    
                    Properties.Add((propertyName, parameterName, type.ToString()));
                }

                Usings = Usings.Where(x => x != Namespace).ToList();
            }
        }
    }
}