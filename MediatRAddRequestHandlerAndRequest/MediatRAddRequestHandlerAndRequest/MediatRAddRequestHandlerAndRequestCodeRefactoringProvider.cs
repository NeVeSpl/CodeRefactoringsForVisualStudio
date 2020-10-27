using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MediatRAddRequestHandlerAndRequest
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MediatRAddRequestHandlerAndRequestCodeRefactoringProvider)), Shared]
    internal class MediatRAddRequestHandlerAndRequestCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);
         
            var typeDecl = node as MethodDeclarationSyntax;
            if (typeDecl == null)
            {
                return;
            }

            var addRequest = CodeAction.Create("Add IRequest<> (empty)", c => Add(context.Document, typeDecl, c, Mode.AddRequest));
            var addRequestHandler = CodeAction.Create("Add IRequestHandler<,> (empty)", c => Add(context.Document, typeDecl, c, Mode.AddRequestHandler));
            var addBoth = CodeAction.Create("Add IRequest<> and IRequestHandler<,> (empty)", c => Add(context.Document, typeDecl, c, Mode.Both));
            var group = CodeAction.Create("MediatR", ImmutableArray.Create(addRequest, addRequestHandler, addBoth), false);

            context.RegisterRefactoring(group);
        }      

        private async Task<Solution> Add(Document document, MethodDeclarationSyntax methodSyntax, CancellationToken cancellationToken, Mode whatToAdd)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax, cancellationToken);

            var folders = new List<string>(document.Folders);
            folders.Add(methodSyntax.Identifier.ValueText);

            if (whatToAdd.HasFlag(Mode.AddRequestHandler))
            {
                var requestHandlerTemplate = new RequestHandlerTemplate(methodSyntax, methodSymbol);
                var handlerSyntax = requestHandlerTemplate.Create();
                document = document.Project.AddDocument(requestHandlerTemplate.HandlerName + ".cs", handlerSyntax, folders);
            }
            if (whatToAdd.HasFlag(Mode.AddRequest))
            {
                var requestTemplate = new RequestTemplate(methodSyntax, methodSymbol);
                var requestSyntax = requestTemplate.Create();
                document = document.Project.AddDocument(requestTemplate.CommandName + ".cs", requestSyntax, folders);
            }

            return document.Project.Solution;
        }


        private enum Mode { AddRequestHandler = 1, AddRequest = 2, Both = 3 }
    }
}