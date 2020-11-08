using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Pluralize.NET;

namespace RenameVariableAfterType
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(RenameVariableAfterTypeCodeRefactoringProvider)), Shared]
    internal class RenameVariableAfterTypeCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        { 
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodes = root.ExtractSelectedNodesOfType<VariableDeclarationSyntax>(context.Span).Where(x => !x.ContainsDiagnostics).ToList();                    
          
            if (!nodes.Any())
            {
                return;
            }
           
            var action = CodeAction.Create("Rename variable after type", c => RenameVariablesAfterType(context.Document, nodes, c));          
            context.RegisterRefactoring(action);
        }

        private async Task<Solution> RenameVariablesAfterType(Document document, IEnumerable<VariableDeclarationSyntax> variableDeclarations, CancellationToken cancellationToken)
        {  
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var solution = document.Project.Solution;

            foreach (var variableDeclaration in variableDeclarations)
            {
                TypeInfo typeInfo = semanticModel.GetTypeInfo(variableDeclaration.Type, cancellationToken);
                foreach (var variableSyntax in variableDeclaration.Variables)
                {
                    string newName = GenerateNewName(typeInfo.Type);
                    ISymbol variableSymbol = semanticModel.GetDeclaredSymbol(variableSyntax, cancellationToken);

                    var optionSet = solution.Workspace.Options;
                    try
                    {
                        solution = await Renamer.RenameSymbolAsync(solution, variableSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        // if solution does not compile, the Renamer may throw exception
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            return solution;
        }

        private string GenerateNewName(ITypeSymbol typeSymbol)
        {
            var type = typeSymbol;
            bool isCollection = false;           
            while (type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType && namedTypeSymbol.TypeArguments.Length > 0)
            {
                if (type.ContainingNamespace?.ToString().StartsWith("System.Collections") == true)
                {
                    isCollection = true;
                }
                type = namedTypeSymbol.TypeArguments.First();
            }

            string name = type.Name;

            if ((name.Length > 1) && (name[0] == 'I') && (char.IsUpper(name[1])))
            {
                name = name.Substring(1);
            }
            var words = name.SplitStringIntoSeparateWords().ToList();

            if (words.Any())
            {
                if (isCollection)
                {
                    try
                    {
                        words[words.Count - 1] = Pluralize(words.Last());
                    }
                    catch
                    {
                        // sometimes VS has problems with loading Pluralize dll
                        words[words.Count - 1] = words.Last() + "s";
                    }
                }
                words[0] = words.First().ToLowerFirst();
            }

            string newName = string.Join("", words);

            return newName;
        }

        private string Pluralize(string word)
        {
            IPluralize pluralizer = new Pluralizer();
            return pluralizer.Pluralize(word);
        }
    }
}