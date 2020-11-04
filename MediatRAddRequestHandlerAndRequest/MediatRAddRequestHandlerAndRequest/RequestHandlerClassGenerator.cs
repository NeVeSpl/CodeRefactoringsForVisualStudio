using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MediatRAddRequestHandlerAndRequest
{
    internal class RequestHandlerClassGenerator
    { 
        public static DocumentTemplate GenerateDocument(BasicData data, IEnumerable<DependecyData> contexDependecies)
        {
            data.Usings.AddRange(new[] {"System.Threading", "System.Threading.Tasks" });
            data.Usings.AddRange(contexDependecies.Select(x => x.Using));

            var documentTemplate = new DocumentTemplate();
            documentTemplate.Syntax = GenerateSyntax(data, contexDependecies);
            documentTemplate.FileName = $"{data.HandlerName}.cs";
            documentTemplate.SolutionFolders = data.SolutionFolders;

            return documentTemplate;
        }

        private static CompilationUnitSyntax GenerateSyntax(BasicData data, IEnumerable<DependecyData> contexDependecies)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit();
            compilationUnit = compilationUnit.AddUsings(data.Usings, data.Namespace);

            var @namespace = SyntaxFactoryEx.NamespaceDeclaration(data.Namespace);
            var classDeclaration = SyntaxFactoryEx.InternalClassDeclaration(data.HandlerName);

            var typeArgumentList = new List<TypeSyntax>();
            typeArgumentList.Add(SyntaxFactory.IdentifierName(data.CommandName + data.CommandTypeArguments));
            if (data.ReturnType != null)
            {
                typeArgumentList.Add(data.ReturnType);
            }
            classDeclaration = classDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.GenericName(SyntaxFactory.Identifier("IRequestHandler")).AddTypeArgumentListArguments(typeArgumentList.ToArray())));
                       
            var constructor = GenerateConstructor(data.HandlerName, contexDependecies);
            var handleMethod = GenerateHandleMethod(data);
            var members = new MemberDeclarationSyntax[] { constructor, handleMethod };

            classDeclaration = classDeclaration.AddMembers(members);
            @namespace = @namespace.AddMembers(classDeclaration);
            compilationUnit = compilationUnit.AddMembers(@namespace);
            var code = compilationUnit.NormalizeWhitespace();

            return code;
        }

        private static ConstructorDeclarationSyntax GenerateConstructor(string name, IEnumerable<DependecyData> contexDependecies)
        {
            var parameters = contexDependecies.Select(x => SyntaxFactoryEx.Parameter(x.Name.ToLowerFirst(), x.Type));
            var constructor = SyntaxFactory.ConstructorDeclaration(SyntaxFactory.Identifier(name))
               .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
               .AddParameterListParameters(parameters.ToArray())
               .WithBody(SyntaxFactory.Block());
               //.AddBodyStatements(SyntaxFactory.Block());
            return constructor;
        }

        private static MethodDeclarationSyntax GenerateHandleMethod(BasicData data)
        {
            var handleMethodReturnType = SyntaxFactory.GenericName("Task").AddTypeArgumentListArguments(data.ReturnType);
            if (data.ReturnType == null)
            {
                handleMethodReturnType = SyntaxFactory.GenericName("Task").AddTypeArgumentListArguments(SyntaxFactory.IdentifierName("Unit"));
            }
            var handleMethod = SyntaxFactory.MethodDeclaration(handleMethodReturnType, "Handle");
            handleMethod = handleMethod.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
            handleMethod = handleMethod.AddParameterListParameters(SyntaxFactoryEx.Parameter(data.CommandParameterNameInHandleMethod, data.CommandName + data.CommandTypeArguments),
                                                                   SyntaxFactoryEx.Parameter("cancellationToken", "CancellationToken"));
            handleMethod = handleMethod.WithBody(SyntaxFactory.Block());
            return handleMethod;
        }
    }
}