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
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Rename;

namespace RenameVariableAfterType
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(RenameVariableAfterTypeCodeRefactoringProvider)), Shared]
    public class RenameVariableAfterTypeCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            SemanticModel semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            CancellationToken cancellationToken = context.CancellationToken;            
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);            

            var variable_nodes = root.ExtractSelectedNodesOfType<VariableDeclarationSyntax>(context.Span).Where(x => !x.ContainsDiagnostics && x.Type != null).ToList();
            var nodesToRename = new List<NodeToRename>();

            if (variable_nodes.Any())
            {
                foreach (var variableDeclaration in variable_nodes)
                {
                    TypeInfo typeInfo = semanticModel.GetTypeInfo(variableDeclaration.Type, cancellationToken);

                    foreach (var variableSyntax in variableDeclaration.Variables)
                    {
                        nodesToRename.Add(new(variableSyntax, typeInfo));
                    }
                }
            }

            if (nodesToRename.Count == 1)
            {
                var node = nodesToRename.First();
                node.GenerateNamePropositions();
                foreach(var name in node.NamePropositions)
                {
                    var action = CodeAction.Create($"Rename to: {name}", c => RenameNode(context.Document, node.SyntaxNode, name, c));
                    context.RegisterRefactoring(action);
                }
            }
            if (nodesToRename.Count > 1)
            {
                var action = CodeAction.Create("Rename variables after type", c => RenameNodes(context.Document, nodesToRename, Mode.AfterType, c));
                context.RegisterRefactoring(action);
            }

            if (nodesToRename.Where(x => x.DoesItHaveExpression).Count() > 1)
            {
                var action = CodeAction.Create("Rename variables after expression", c => RenameNodes(context.Document, nodesToRename, Mode.AfterExpression, c));
                context.RegisterRefactoring(action);
            }

            var toRegisterPropositions = new List<(CSharpSyntaxNode node, string name)>();

            // Handle parameters
            var parameter_node = root.ExtractSelectedNodesOfType<ParameterSyntax>(context.Span).Where(x => !x.ContainsDiagnostics && x.Type != null).FirstOrDefault();
            if (parameter_node is not null)
            {
                var typeInfo = semanticModel.GetTypeInfo(parameter_node.Type, context.CancellationToken);

                var nameAfterType = NameGenerator.GenerateNewNameFromType(typeInfo.Type);
                toRegisterPropositions.Add((parameter_node, nameAfterType));
            }            

            // Handle foreach
            var foreach_node = root.ExtractSelectedNodesOfType<ForEachStatementSyntax>(context.Span).Where(x => !x.ContainsDiagnostics && x.Type != null).FirstOrDefault();
            if (foreach_node is not null)
            {
                var typeInfo = semanticModel.GetTypeInfo(foreach_node.Type, context.CancellationToken);
                var nameAfterType = NameGenerator.Singularize(NameGenerator.GenerateNewNameFromType(typeInfo.Type));
                toRegisterPropositions.Add((foreach_node, nameAfterType));

                var nameAfterExpression = NameGenerator.Singularize(NameGenerator.GenerateNewNameFromExpression(foreach_node.Expression));
                toRegisterPropositions.Add((foreach_node, nameAfterExpression));
            }

            // Handle arguments
            var argument_node = root.ExtractSelectedNodesOfType<ArgumentSyntax>(context.Span).Where(x => !x.ContainsDiagnostics).FirstOrDefault();
            if (argument_node is not null)
            {                
                var operation = semanticModel.GetOperation(argument_node);
                var parameter = (operation as IArgumentOperation)?.Parameter;

                if (parameter != null) 
                {
                    toRegisterPropositions.Add((argument_node, parameter.Name));                    

                    var nameAfterType = NameGenerator.GenerateNewNameFromType(parameter.Type);
                    toRegisterPropositions.Add((argument_node, nameAfterType));                   
                }
            }

            // Register refactorings
            foreach (var proposition in toRegisterPropositions)
            {
                if (string.IsNullOrEmpty(proposition.name)) 
                    continue;

                context.RegisterRefactoring(CodeAction.Create($"Rename to: {proposition.name}", c => RenameNode(context.Document, proposition.node, proposition.name, c)));
            }
        }

        enum Mode { AfterType, AfterExpression }
        private async Task<Solution> RenameNode(Document document, CSharpSyntaxNode cSharpSyntaxNode, string newName, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var solution = document.Project.Solution;

            solution = await DoRename(cSharpSyntaxNode, newName, semanticModel, solution, cancellationToken).ConfigureAwait(false);

            return solution;
        }
        private async Task<Solution> RenameNodes(Document document, IEnumerable<NodeToRename> nodesToRename, Mode mode, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var solution = document.Project.Solution;

            foreach (var nodeToRename in nodesToRename)
            {
                nodeToRename.GenerateNamePropositions();
                string newName = (mode == Mode.AfterType ? nodeToRename.NameAfterType : nodeToRename.NameAfterExpression) ?? nodeToRename.NameAfterType;

                solution = await DoRename(nodeToRename.SyntaxNode, newName, semanticModel, solution, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();
            }

            return solution;
        }
        private async Task<Solution> DoRename(CSharpSyntaxNode syntaxNode, string newName, SemanticModel semanticModel, Solution solution, CancellationToken cancellationToken)
        {
            try
            {
                ISymbol symbolToRename = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
                solution = await Renamer.RenameSymbolAsync(solution, symbolToRename, newName, solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // if solution does not compile, the Renamer may throw
            }

            return solution;
        }
              

        class NodeToRename
        {
            private readonly TypeInfo typeInfo;
            private bool wereNamesGenerated;
            private ExpressionSyntax expressionSyntax;
            public CSharpSyntaxNode SyntaxNode { get; }
            public bool DoesItHaveExpression { get => expressionSyntax != null; }
            public IEnumerable<string> NamePropositions { get; private set; }
            public string NameAfterType { get; private set; }
            public string NameAfterExpression { get; private set; }


            public NodeToRename(CSharpSyntaxNode syntaxNodeToRename, TypeInfo typeInfo)
            {
                this.typeInfo = typeInfo;
                this.SyntaxNode = syntaxNodeToRename;
                this.expressionSyntax = SyntaxNode.DescendantNodes().Where(y => y is InvocationExpressionSyntax || y is MemberAccessExpressionSyntax).OfType<ExpressionSyntax>().FirstOrDefault();            
            }


            public void GenerateNamePropositions()
            {
                if (wereNamesGenerated) return; 
                NamePropositions = GenerateNamePropositionsInternal().ToArray();
                wereNamesGenerated = true;
            }

            private IEnumerable<string> GenerateNamePropositionsInternal()
            {
                NameAfterType = NameGenerator.GenerateNewNameFromType(typeInfo.Type);
                yield return NameAfterType;
                NameAfterExpression = NameGenerator.GenerateNewNameFromExpression(expressionSyntax);
                if (!string.IsNullOrEmpty(NameAfterExpression))
                {
                    yield return NameAfterExpression;
                }
            }
        }
    }
}