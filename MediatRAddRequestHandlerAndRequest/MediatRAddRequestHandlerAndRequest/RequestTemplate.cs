using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MediatRAddRequestHandlerAndRequest
{
    internal class RequestTemplate
    { 
        public string CommandName { get; }
        public TypeSyntax ReturnType { get; }       
        public List<string> Usings { get; }
        public string Namespace { get; }


        public RequestTemplate(MethodDeclarationSyntax method, IMethodSymbol methodSymbol)
        {            
            CommandName = method.Identifier.ValueText + "Command";          
            ReturnType = UnpackTypeFromTaskAndActionResult(method.ReturnType);         

            if ((method.ReturnType is PredefinedTypeSyntax pred) && (pred.Keyword.ValueText == "void"))
            {
                ReturnType = null;              
            }

            Usings = new List<string>() { "MediatR" };
            Namespace = $"{methodSymbol.ContainingNamespace}.{method.Identifier.ValueText}";
        }

        private TypeSyntax UnpackTypeFromTaskAndActionResult(TypeSyntax type)
        {
            if ((type is GenericNameSyntax generic) && (generic.Identifier.ValueText == "Task") && generic.TypeArgumentList.Arguments.Count == 1)
            {
                type = generic.TypeArgumentList.Arguments.First();
                if ((type is GenericNameSyntax generic2) && (generic2.Identifier.ValueText == "ActionResult") && generic2.TypeArgumentList.Arguments.Count == 1)
                {
                    type = generic2.TypeArgumentList.Arguments.First();
                }
            }
            return type;
        }

        public CompilationUnitSyntax Create()
        {
            var syntaxFactory = SyntaxFactory.CompilationUnit();

            syntaxFactory = syntaxFactory.AddUsings(Usings.Select(x => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(x))).ToArray());
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(Namespace)).NormalizeWhitespace();

            var classDeclaration = SyntaxFactory.ClassDeclaration(CommandName);
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
          
            if (ReturnType != null)
            {               
                classDeclaration = classDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.GenericName(SyntaxFactory.Identifier("IRequest")).AddTypeArgumentListArguments(ReturnType)));
            }
            else
            {
                classDeclaration = classDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("IRequest")));
            }
      
            @namespace = @namespace.AddMembers(classDeclaration);
            syntaxFactory = syntaxFactory.AddMembers(@namespace);
            var code = syntaxFactory.NormalizeWhitespace();

            return code;
        }
    }
}