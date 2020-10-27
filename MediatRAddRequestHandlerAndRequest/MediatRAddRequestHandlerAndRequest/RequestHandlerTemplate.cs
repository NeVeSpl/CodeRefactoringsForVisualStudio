using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MediatRAddRequestHandlerAndRequest
{    
    internal class RequestHandlerTemplate
    {
        public string ParameterName { get; }
        public string HandlerName { get;  }
        public string CommandName { get; }
        public TypeSyntax ReturnType { get; }
        public TypeSyntax ReturnTypeWithTask { get; }
        public List<string> Usings { get; }
        public string Namespace { get; }


        public RequestHandlerTemplate(MethodDeclarationSyntax method, IMethodSymbol methodSymbol)
        {
            HandlerName = method.Identifier.ValueText + "Handler";
            CommandName = method.Identifier.ValueText + "Command";
            ParameterName = method.Identifier.ValueText.ToLowerFirst();  
            ReturnType = UnpackTypeFromTaskAndActionResult(method.ReturnType);
            ReturnTypeWithTask = SyntaxFactory.GenericName("Task").AddTypeArgumentListArguments(ReturnType);
                    
            if ((method.ReturnType is PredefinedTypeSyntax pred) && (pred.Keyword.ValueText == "void"))
            {
                ReturnType = null;
                ReturnTypeWithTask = SyntaxFactory.GenericName("Task").AddTypeArgumentListArguments(SyntaxFactory.IdentifierName("Unit"));
            }                

            Usings = new List<string>() { "System.Threading", "System.Threading.Tasks", "MediatR" };
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

            var classDeclaration = SyntaxFactory.ClassDeclaration(HandlerName);
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword));

            List<TypeSyntax> typeArgumentList = new List<TypeSyntax>();
            typeArgumentList.Add(SyntaxFactory.IdentifierName(CommandName));
            if (ReturnType != null)
            {
               
                typeArgumentList.Add(ReturnType);
            }
            classDeclaration = classDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.GenericName(SyntaxFactory.Identifier("IRequestHandler")).AddTypeArgumentListArguments(typeArgumentList.ToArray())));

            var members = new List<MemberDeclarationSyntax>();
            var handler = SyntaxFactory.MethodDeclaration(ReturnTypeWithTask, "Handle");
            handler = handler.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
            handler = handler.AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier(ParameterName)).WithType(SyntaxFactory.IdentifierName(CommandName)),
                                                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken")).WithType(SyntaxFactory.IdentifierName("CancellationToken")));
            handler = handler.WithBody(SyntaxFactory.Block());
            members.Add(handler);

            classDeclaration = classDeclaration.AddMembers(members.ToArray());
            @namespace = @namespace.AddMembers(classDeclaration);
            syntaxFactory = syntaxFactory.AddMembers(@namespace);
            var code = syntaxFactory.NormalizeWhitespace();

            return code;
        }
    }
    
}
