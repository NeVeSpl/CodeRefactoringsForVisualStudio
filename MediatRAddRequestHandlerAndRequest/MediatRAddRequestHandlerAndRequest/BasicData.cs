using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace MediatRAddRequestHandlerAndRequest
{
    internal sealed class BasicData
    {
        public string CommandName { get; set; }
        public string CommandTypeArguments { get; set; }
        public string HandlerName { get; set; }
        public string CommandParameterNameInHandleMethod { get; set; }
        public TypeSyntax ReturnType { get; set; }
        public List<string> Usings { get; set; }
        public string Namespace { get; set; }
        public IEnumerable<string> SolutionFolders { get; set; }


        public static async Task<BasicData> GetFromMethodDeclaration(Solution solution, MemberDeclarationSyntax method, CancellationToken token)
        {
            var document = solution.GetDocument(method.SyntaxTree);
            var semanticModel = await document.GetSemanticModelAsync(token).ConfigureAwait(false);
            var symbol = semanticModel.GetDeclaredSymbol(method, token);
            var methodSymbol = symbol as IMethodSymbol;
            var typeSymbol = symbol as ITypeSymbol;

            var existingImplementationOfIRequest = typeSymbol;
            if (methodSymbol != null)
            {
                existingImplementationOfIRequest = GetParameterTypeThatImplementsIRequest(methodSymbol);
            }

            var result = new BasicData();
            string basicName = null;
            ITypeSymbol returnTypeSymbol = null;
            List<string> usings = new List<string>() { "MediatR" };
            
            if (existingImplementationOfIRequest != null)
            {
                result.CommandName = existingImplementationOfIRequest.Name;
                var implementedInterface = GetIRequestSymbol(existingImplementationOfIRequest.AllInterfaces);                
                if (implementedInterface?.TypeArguments.Length == 1)
                {
                    returnTypeSymbol = implementedInterface.TypeArguments.First(); 
                }

                result.Namespace = existingImplementationOfIRequest.ContainingNamespace.ToString();
                var documentOfExistingImplementation = solution.GetDocument(existingImplementationOfIRequest.DeclaringSyntaxReferences.First().SyntaxTree);
                result.SolutionFolders = documentOfExistingImplementation.Folders;

                string fullName = existingImplementationOfIRequest.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                if (existingImplementationOfIRequest is INamedTypeSymbol namedTypeSymbol)
                {
                    foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                    {
                        if (!(typeArgument is ITypeParameterSymbol))
                        {
                            usings.AddRange(typeArgument.GetUsings());
                        }
                    }
                }
                result.CommandTypeArguments = fullName.Substring(result.CommandName.Length);
                basicName = result.CommandName.RemovePostfix("Query", "Command");
            }
            else
            {                
                basicName = methodSymbol.Name;
                returnTypeSymbol = methodSymbol.ReturnType.UnpackTypeFromTaskAndActionResult();                
                result.Namespace = $"{methodSymbol.ContainingNamespace}.{basicName}";               
                result.SolutionFolders = new List<string>(document.Folders) { basicName };
                result.CommandName = basicName + "Command";
            }

            if ((returnTypeSymbol != null) && (returnTypeSymbol.SpecialType != SpecialType.System_Void))
            {
                result.ReturnType = SyntaxFactory.IdentifierName(returnTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            }
           
            usings.AddRange(returnTypeSymbol.GetUsings());
            result.Usings = usings.Where(x => x != result.Namespace).ToList();  
            result.HandlerName = basicName + "Handler";
            result.CommandParameterNameInHandleMethod = "command";
           
            return result;
        }

        private static ITypeSymbol GetParameterTypeThatImplementsIRequest(IMethodSymbol method)
        {
            return method.Parameters.Where(x => GetIRequestSymbol(x.Type.AllInterfaces) != null).Select(x => x.Type).FirstOrDefault();           
        }       
        private static INamedTypeSymbol GetIRequestSymbol(IEnumerable<INamedTypeSymbol> interfaces)
        {
            return interfaces.Where(x => x.ToString().StartsWith("MediatR.IRequest")).FirstOrDefault();
        }
    }    
}