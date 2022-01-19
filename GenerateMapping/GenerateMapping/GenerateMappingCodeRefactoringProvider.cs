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
using GenerateMapping.Model;

namespace GenerateMapping
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(GenerateMappingCodeRefactoringProvider)), Shared]
    public class GenerateMappingCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {   
            var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var selectedMethodDeclarations = rootNode.ExtractSelectedNodesOfType<MethodDeclarationSyntax>(context.Span);
            var selectedConstructorDeclarations = rootNode.ExtractSelectedNodesOfType<ConstructorDeclarationSyntax>(context.Span);
            var selected = selectedMethodDeclarations.OfType<BaseMethodDeclarationSyntax>().Union(selectedConstructorDeclarations);

            if (selected.Any())
            {
                var action = CodeAction.Create("Generate mapping", c => GenerateMapping(context.Document, selected.First(), c));
                context.RegisterRefactoring(action);
            }
        }

        private async Task<Document> GenerateMapping(Document document, BaseMethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
           
            var syntaxTree = methodDeclaration.SyntaxTree;
            var semanticModel = await solution.GetDocument(syntaxTree).GetSemanticModelAsync();
            IMethodSymbol methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);

            var leftAccessors = GetLeftAccessor(methodSymbol).ToList();
            var rightAccessors = GetRightAccessors(methodSymbol).ToList();

            var matches = DoMatching(leftAccessors, rightAccessors);

            var updatedMethod = MappingSyntaxGenerator.GenerateSyntax(methodDeclaration, matches, leftAccessors.First());
            SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            rootNode = rootNode.ReplaceNode(methodDeclaration, updatedMethod);
            return document.WithSyntaxRoot(rootNode);
        }
     

        private IEnumerable<Accessor> GetLeftAccessor(IMethodSymbol methodSymbol)
        {
            if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Void && methodSymbol.IsStatic == false)
            {
                yield return new Accessor(methodSymbol.ContainingType, "this", false);
            }
            else
            {
                yield return new Accessor(methodSymbol.ReturnType, "result", true);
            }
        }
        private IEnumerable<Accessor> GetRightAccessors(IMethodSymbol methodSymbol)
        {
            if (methodSymbol.Parameters.Any())
            {
                foreach (var parameter in methodSymbol.Parameters)
                {
                    yield return  new Accessor(parameter);
                }
            }
            else
            {
                yield return new Accessor(methodSymbol.ContainingType, "this", false);
            }
        }
        private IEnumerable<Match> DoMatching(List<Accessor> leftAccessors, List<Accessor> rightAccessors)
        {
            var results = new List<Match>();

            foreach (MatchLevel level in Enum.GetValues(typeof(MatchLevel)))
            {
                foreach (var leftAccessor in StreamAccessors(leftAccessors))
                {
                    foreach (var rightAccessor in StreamAccessors(rightAccessors))
                    {
                        bool isMatch = false;

                        switch (level)
                        {
                            case MatchLevel.Perfect:
                                isMatch = leftAccessor.Name == rightAccessor.Name;
                                break;
                            case MatchLevel.IgnoreCase:
                                isMatch = String.Equals(leftAccessor.Name, rightAccessor.Name, StringComparison.OrdinalIgnoreCase);
                                break;
                            case MatchLevel.WithoutPrefix:
                                var leftName = leftAccessor.Name.WithoutPrefix();
                                var rightName = rightAccessor.Name.WithoutPrefix();
                                isMatch = String.Equals(leftName, rightName, StringComparison.OrdinalIgnoreCase);
                                break;
                        }

                        if (isMatch)
                        {
                            results.Add(new Match(leftAccessor, rightAccessor));
                            leftAccessor.IsMatched = true;
                            rightAccessor.IsMatched = true;
                        }
                    }
                }
            }

            return results;            
        }

      

        private IEnumerable<Accessor> StreamAccessors(IEnumerable<Accessor> accessors)
        {
            foreach (var accessor in accessors)
            {
                if (accessor.IsMatched == false)
                {
                    yield return accessor;
                }
            }
            foreach (var accessor in accessors)
            {
                foreach (var child in StreamAccessors(accessor.Children))
                {
                    if (accessor.IsMatched == false)
                    { 
                        yield return child;
                    }
                }
            }
        }
    }

    enum MatchLevel { Perfect, IgnoreCase, WithoutPrefix, None }
    public class Match
    {
        public Accessor LeftAccessor { get; }
        public Accessor RightAccessor { get; }


        public Match(Accessor leftAccessor, Accessor rightAccessor)
        {
            LeftAccessor = leftAccessor;
            RightAccessor = rightAccessor;
        }       
    }


    public class Accessor
    {
        public readonly TypeData Type;
        

        public Accessor Parent { get; }
        public string Name { get;  }
        public IEnumerable<Accessor> Children { get;  } = Enumerable.Empty<Accessor>();
        public bool IsMatched { get; set; }
        


        public Accessor(IParameterSymbol parameter)
        {
            Type = new TypeData(parameter.Type);
            Name = parameter.Name;
            Children = GetAccessorsForType(parameter.Type, true);
        }

        public Accessor(ITypeSymbol type, string name, bool publicOnly)
        {
            Type = new TypeData(type);
            Name = name;
            Children = GetAccessorsForType(type, publicOnly);
        }        

        public Accessor(ISymbol symbol, Accessor parent)
        {
            if (symbol is IPropertySymbol property)
            {
                Type = new TypeData(property.Type);
                Name = property.Name;
            }
            if (symbol is IFieldSymbol field)
            {
                Type = new TypeData(field.Type);
                Name = field.Name;
            }
            Parent = parent;
        }



      


        private IEnumerable<Accessor> GetAccessorsForType(ITypeSymbol type, bool publicOnly)
        {
            var fields = type.GetMembers().OfType<IFieldSymbol>().OfType<ISymbol>();
            var props = type.GetMembers().OfType<IPropertySymbol>().OfType<ISymbol>();
            var all = fields.Union(props);
            var filtered = all.Where(x => !x.IsCompilerGenerated() && !x.IsStatic);
            if (publicOnly)
            {
                filtered = filtered.Where(x => x.DeclaredAccessibility == Accessibility.Public);
            }
            var transformed = filtered.Select(x => new Accessor(x, this));

            return transformed.ToList();
        }
    }
}