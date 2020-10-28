using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MediatRAddRequestHandlerAndRequest
{
    internal class RequestClassGenerator
    {
        internal class RequestClassData
        {
            public string CommandName { get; }
            public TypeSyntax ReturnType { get; }
            public List<string> Usings { get; }
            public string Namespace { get; }


            public RequestClassData(MethodDeclarationSyntax method, IMethodSymbol methodSymbol)
            {
                CommandName = method.Identifier.ValueText + "Command";
                ReturnType = method.ReturnType.UnpackTypeFromTaskAndActionResult();

                if ((method.ReturnType is PredefinedTypeSyntax pred) && (pred.Keyword.ValueText == "void"))
                {
                    ReturnType = null;
                }

                Namespace = $"{methodSymbol.ContainingNamespace}.{method.Identifier.ValueText}";
                Usings = new List<string>(methodSymbol.ReturnType.GetUsings()) { "MediatR" }.Where(x => x != Namespace).ToList();
            }
        }
       

        public static async Task<DocumentTemplate> GenerateDocument(Solution solution, MethodDeclarationSyntax method, CancellationToken token)
        {
            var document = solution.GetDocument(method.SyntaxTree);
            var semanticModel  = await document.GetSemanticModelAsync(token).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetDeclaredSymbol(method, token);

            var folders = new List<string>(document.Folders);
            folders.Add(method.Identifier.ValueText);

            var data = new RequestClassData(method, methodSymbol);

            var documentTemplate = new DocumentTemplate();
            documentTemplate.Syntax = GenerateSyntax(data);
            documentTemplate.FileName = $"{data.CommandName}.cs";
            documentTemplate.SolutionFolders = folders;

            return documentTemplate;
        }

        private static CompilationUnitSyntax GenerateSyntax(RequestClassData data)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit();
            compilationUnit = compilationUnit.AddUsings(data.Usings);

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