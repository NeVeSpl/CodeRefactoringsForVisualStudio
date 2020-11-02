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
    internal class BasicData
    {
        public string CommandName { get; set; }
        public string CommandTypeArguments { get; set; }
        public string HandlerName { get; set; }
        public string CommandParameterNameInHandleMethod { get; set; }
        public TypeSyntax ReturnType { get; set; }
        public List<string> Usings { get; set; }
        public string Namespace { get; set; }
        public IEnumerable<string> SolutionFolders { get; set; }


        public static async Task<BasicData> GetFromMethodDeclaration(Solution solution, MethodDeclarationSyntax method, CancellationToken token)
        {
            var document = solution.GetDocument(method.SyntaxTree);
            var semanticModel = await document.GetSemanticModelAsync(token).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetDeclaredSymbol(method, token) as IMethodSymbol;

            var result = new BasicData();

            string basicName = null;
            ITypeSymbol returnTypeSymbol = null;
            var (existingImplementationOfIRequest, implementedInterface) = GetParameterTypeThatImplementsIRequest(methodSymbol);
            if (existingImplementationOfIRequest != null)
            {
                result.CommandName = existingImplementationOfIRequest.Name;
                if (implementedInterface.TypeArguments.Length == 1)
                {
                    returnTypeSymbol = implementedInterface.TypeArguments.First();                   
                    result.ReturnType = SyntaxFactory.IdentifierName(returnTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                }

                result.Namespace = existingImplementationOfIRequest.ContainingNamespace.ToString();
                var documentOfExistingImplementation = solution.GetDocument(existingImplementationOfIRequest.DeclaringSyntaxReferences.First().SyntaxTree);
                result.SolutionFolders = documentOfExistingImplementation.Folders;
                //string fullName = existingImplementationOfIRequest.ToDisplayString(new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Omitted,
                //                                                                              SymbolDisplayTypeQualificationStyle.NameOnly,
                //                                                                              SymbolDisplayGenericsOptions.IncludeTypeParameters,
                //                                                                              miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes));
                string fullName = existingImplementationOfIRequest.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                result.CommandTypeArguments = fullName.Substring(result.CommandName.Length);
                basicName = result.CommandName.RemoveSufix("Query", "Command");
            }
            else
            {
                basicName = method.Identifier.ValueText;
                returnTypeSymbol = methodSymbol.ReturnType;
                result.ReturnType = method.ReturnType.UnpackTypeFromTaskAndActionResult();
                result.Namespace = $"{methodSymbol.ContainingNamespace}.{basicName}";               
                result.SolutionFolders = new List<string>(document.Folders) { basicName };
                result.CommandName = basicName + "Command";
            }

            if ((result.ReturnType is PredefinedTypeSyntax pred) && (pred.Keyword.ValueText == "void"))
            {
                result.ReturnType = null;
            }

            var unpackedReturnTypeSymbol = returnTypeSymbol.UnpackTypeFromTaskAndActionResult();
            result.Usings = new List<string>(unpackedReturnTypeSymbol.GetUsings()) { "MediatR" }.Where(x => x != result.Namespace).ToList();  
            result.HandlerName = basicName + "Handler";
            result.CommandParameterNameInHandleMethod = "command";
           
            return result;
        }

        private static (ITypeSymbol type, INamedTypeSymbol interfaceType) GetParameterTypeThatImplementsIRequest(IMethodSymbol method)
        {
            foreach (var parameter in method.Parameters)
            {
                foreach (var @interface in parameter.Type.AllInterfaces)
                {
                    var interfaceName = @interface.ToString();
                    if (interfaceName.StartsWith("MediatR.IRequest"))
                    {
                        return (parameter.Type, @interface);
                    }
                }
            }
            return (null, null);
        }       
    }    
}