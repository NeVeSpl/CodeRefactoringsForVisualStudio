using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MediatRAddRequestHandlerAndRequest
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MediatRAddRequestHandlerAndRequestCodeRefactoringProvider)), Shared]
    internal class MediatRAddRequestHandlerAndRequestCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var methodDeclarations = root.ExtractSelectedNodesOfType<MethodDeclarationSyntax>(context.Span, true);
            
            if (!methodDeclarations.Any())
            {
                return;
            }

            var addRequest = CodeAction.Create("Add IRequest<> (empty)", c => Add(context.Document, methodDeclarations.First(), c, Mode.AddRequest));
            var addRequestHandler = CodeAction.Create("Add IRequestHandler<,> (empty)", c => Add(context.Document, methodDeclarations.First(), c, Mode.AddRequestHandler));
            var addBoth = CodeAction.Create("Add IRequest<> and IRequestHandler<,> (empty)", c => Add(context.Document, methodDeclarations.First(), c, Mode.Both));
            var group = CodeAction.Create("MediatR", ImmutableArray.Create(addRequest, addRequestHandler, addBoth), false);

            context.RegisterRefactoring(group);
        }      

        private async Task<Solution> Add(Document document, MethodDeclarationSyntax methodSyntax, CancellationToken cancellationToken, Mode whatToAdd)
        {
            if (whatToAdd.HasFlag(Mode.AddRequestHandler))
            {
                var requestHandlerDocument = await RequestHandlerClassGenerator.GenerateDocument(document.Project.Solution, methodSyntax, cancellationToken);             
                document = document.Project.AddDocument(requestHandlerDocument);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (whatToAdd.HasFlag(Mode.AddRequest))
            {                
                var requestClassDocument = await RequestClassGenerator.GenerateDocument(document.Project.Solution, methodSyntax, cancellationToken);
                document = document.Project.AddDocument(requestClassDocument);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return document.Project.Solution;
        }

        private enum Mode { AddRequestHandler = 1, AddRequest = 2, Both = 3 }
    }
}