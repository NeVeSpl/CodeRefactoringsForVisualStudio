using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace MediatRAddRequestHandlerAndRequest
{
    internal class RequestClassGenerator
    { 
        public static DocumentTemplate GenerateDocument(BasicData data)
        {
            var documentTemplate = new DocumentTemplate();
            documentTemplate.Syntax = GenerateSyntax(data);
            documentTemplate.FileName = $"{data.CommandName}.cs";
            documentTemplate.SolutionFolders = data.SolutionFolders;
            return documentTemplate;
        }

        private static CompilationUnitSyntax GenerateSyntax(BasicData data)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit();
            compilationUnit = compilationUnit.AddUsings(data.Usings, data.Namespace);

            var @namespace = SyntaxFactoryEx.NamespaceDeclaration(data.Namespace);
            var classDeclaration = SyntaxFactoryEx.PublicClassDeclaration(data.CommandName);

            if (data.ReturnType != null)
            {               
                classDeclaration = classDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.GenericName(SyntaxFactory.Identifier("IRequest")).AddTypeArgumentListArguments(data.ReturnType)));
            }
            else
            {
                classDeclaration = classDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("IRequest")));
            }
      
            @namespace = @namespace.AddMembers(classDeclaration);
            compilationUnit = compilationUnit.AddMembers(@namespace);
            var code = compilationUnit.NormalizeWhitespace();
            
            return code;
        }
    }
}