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
    internal class RequestHandlerClassGenerator
    {
        internal class RequestHandlerData
        {
            public string ParameterName { get; }
            public string HandlerName { get; }
            public string CommandName { get; }
            public string CommandNameWithTypeArguments { get; }
            public TypeSyntax ReturnType { get; }
            public TypeSyntax ReturnTypeWithTask { get; }
            public List<string> Usings { get; }
            public string Namespace { get; }


            public RequestHandlerData(MethodDeclarationSyntax method, IMethodSymbol methodSymbol)
            {
                HandlerName = method.Identifier.ValueText + "Handler";
                (CommandName, CommandNameWithTypeArguments, Namespace) = GetCommandData(methodSymbol);
                ParameterName = "command";

                ReturnType = method.ReturnType.UnpackTypeFromTaskAndActionResult();
                ReturnTypeWithTask = SyntaxFactory.GenericName("Task").AddTypeArgumentListArguments(ReturnType);

                if ((method.ReturnType is PredefinedTypeSyntax pred) && (pred.Keyword.ValueText == "void"))
                {
                    ReturnType = null;
                    ReturnTypeWithTask = SyntaxFactory.GenericName("Task").AddTypeArgumentListArguments(SyntaxFactory.IdentifierName("Unit"));
                }

                Usings = new List<string>(methodSymbol.ReturnType.GetUsings()) { "System.Threading", "System.Threading.Tasks", "MediatR" }.Where(x => x != Namespace).ToList();
            }

            private static (string name, string nameWithTypes, string @namespace) GetCommandData(IMethodSymbol method)
            {
                foreach (var parameter in method.Parameters)
                {
                    foreach (var @interface in parameter.Type.AllInterfaces)
                    {
                        var interfaceName = @interface.ToString();
                        if (interfaceName.StartsWith("MediatR.IRequest"))
                        {
                            string name = parameter.Type.ToDisplayString(new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Omitted,
                                                                                                  SymbolDisplayTypeQualificationStyle.NameOnly,
                                                                                                  SymbolDisplayGenericsOptions.IncludeTypeParameters,
                                                                                                  miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes));
                            return (parameter.Type.Name, name, parameter.Type.ContainingNamespace.ToString());
                        }
                    }
                }
                return (method.Name + "Command", method.Name + "Command", $"{method.ContainingNamespace}.{method.Name}");
            }
        }


        public static async Task<DocumentTemplate> GenerateDocument(Solution solution, MethodDeclarationSyntax method, CancellationToken token)
        {
            var document = solution.GetDocument(method.SyntaxTree);
            var semanticModel = await document.GetSemanticModelAsync(token).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetDeclaredSymbol(method, token);          

            var data = new RequestHandlerData(method, methodSymbol);

            var documentTemplate = new DocumentTemplate();
            documentTemplate.Syntax = GenerateSyntax(data);
            documentTemplate.FileName = $"{data.HandlerName}.cs";
            documentTemplate.SolutionFolders = GetFolders(solution, methodSymbol);

            return documentTemplate;
        }

        private static CompilationUnitSyntax GenerateSyntax(RequestHandlerData data)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit();
            compilationUnit = compilationUnit.AddUsings(data.Usings);

            var @namespace = SyntaxFactoryEx.NamespaceDeclaration(data.Namespace);
            var classDeclaration = SyntaxFactoryEx.InternalClassDeclaration(data.HandlerName);

            var typeArgumentList = new List<TypeSyntax>();
            typeArgumentList.Add(SyntaxFactory.IdentifierName(data.CommandNameWithTypeArguments));
            if (data.ReturnType != null)
            {
                typeArgumentList.Add(data.ReturnType);
            }
            classDeclaration = classDeclaration.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.GenericName(SyntaxFactory.Identifier("IRequestHandler")).AddTypeArgumentListArguments(typeArgumentList.ToArray())));

            var members = new List<MemberDeclarationSyntax>();
            var method = SyntaxFactory.MethodDeclaration(data.ReturnTypeWithTask, "Handle");
            method = method.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
            method = method.AddParameterListParameters(SyntaxFactoryEx.Parameter(data.ParameterName, data.CommandNameWithTypeArguments),
                                                       SyntaxFactoryEx.Parameter("cancellationToken", "CancellationToken"));
            method = method.WithBody(SyntaxFactory.Block());
            members.Add(method);

            classDeclaration = classDeclaration.AddMembers(members.ToArray());
            @namespace = @namespace.AddMembers(classDeclaration);
            compilationUnit = compilationUnit.AddMembers(@namespace);
            var code = compilationUnit.NormalizeWhitespace();

            return code;
        }

        private static IEnumerable<string> GetFolders(Solution solution, IMethodSymbol method)
        {
            foreach (var parameter in method.Parameters)
            {
                foreach (var @interface in parameter.Type.AllInterfaces)
                {
                    var interfaceName = @interface.ToString();
                    if (interfaceName.StartsWith("MediatR.IRequest"))
                    {
                        var commandDocument = solution.GetDocument(parameter.Type.DeclaringSyntaxReferences.First().SyntaxTree);
                        return commandDocument.Folders;
                    }
                }
            }

            var document = solution.GetDocument(method.DeclaringSyntaxReferences.First().SyntaxTree);
            var folders = new List<string>(document.Folders);
            folders.Add(method.Name);
            return folders;
        }
    }
}