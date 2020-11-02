using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MediatRAddRequestHandlerAndRequest
{
    internal class RequestHandlerClassGenerator
    { 
        public static DocumentTemplate GenerateDocument(BasicData data)
        {
            data.Usings.AddRange(new[] {"System.Threading", "System.Threading.Tasks" });

            var documentTemplate = new DocumentTemplate();
            documentTemplate.Syntax = GenerateSyntax(data);
            documentTemplate.FileName = $"{data.HandlerName}.cs";
            documentTemplate.SolutionFolders = data.SolutionFolders;

            return documentTemplate;
        }

        private static CompilationUnitSyntax GenerateSyntax(BasicData data)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit();
            compilationUnit = compilationUnit.AddUsings(data.Usings);

            var @namespace = SyntaxFactoryEx.NamespaceDeclaration(data.Namespace);
            var classDeclaration = SyntaxFactoryEx.InternalClassDeclaration(data.HandlerName);

            var typeArgumentList = new List<TypeSyntax>();
            typeArgumentList.Add(SyntaxFactory.IdentifierName(data.CommandName + data.CommandTypeArguments));
            if (data.ReturnType != null)
            {
                typeArgumentList.Add(data.ReturnType);
            }
            classDeclaration = classDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.GenericName(SyntaxFactory.Identifier("IRequestHandler")).AddTypeArgumentListArguments(typeArgumentList.ToArray())));

            var members = new List<MemberDeclarationSyntax>();
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
            members.Add(handleMethod);

            classDeclaration = classDeclaration.AddMembers(members.ToArray());
            @namespace = @namespace.AddMembers(classDeclaration);
            compilationUnit = compilationUnit.AddMembers(@namespace);
            var code = compilationUnit.NormalizeWhitespace();

            return code;
        }       
    }
}