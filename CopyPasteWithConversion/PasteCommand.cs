using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CopyPasteWithConversion
{
    internal sealed class PasteCommand
    {       
        public enum CommandMode 
        {
            PasteAsSeparateWords = 0x0200,
            PasteAsCamelCase = 0x0210,
            PasteAsPascalCase = 0x220,
            PasteAsSnakeCase = 0x0230
        }
        
        private static readonly Guid CommandSet = new Guid("d1ce5fd5-aeb1-4684-b517-cb32df838c11");
        private readonly int CommandId;
        private readonly AsyncPackage package;
        private readonly CommandMode mode;

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PasteCommand(AsyncPackage package, OleMenuCommandService commandService, CommandMode mode)
        {
            this.mode = mode;
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            CommandId = (int)mode;
            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);       
            commandService.AddCommand(menuItem);
        }       
          
        public static DTE DteInstance
        {
            get;
            private set;
        }

      
        public static async Task InitializeAsync(AsyncPackage package, CommandMode mode)
        {            
            // Switch to the main thread - the call to AddCommand in CopyAsSeparateWords's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            DteInstance = await package.GetServiceAsync(typeof(DTE)) as DTE;
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            new PasteCommand(package, commandService, mode);
        }

      
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            var textDocument = DteInstance.ActiveDocument.Object("TextDocument") as TextDocument;
            if (textDocument != null)
            {
                string textFromClipboard = null;
                try
                {
                     textFromClipboard = Clipboard.GetText();
                }
                catch (ExternalException)
                {

                }
                if (textFromClipboard == null) return;

                var words = textFromClipboard.SplitStringIntoSeparateWords();
                var result = String.Empty;
                switch (mode)
                {
                    case CommandMode.PasteAsSeparateWords:
                        result = String.Join(" ", words);
                        break;
                    case CommandMode.PasteAsCamelCase:
                        result = string.Join("", words.Select(x => x.ToLower().ToUpperFirst())).ToLowerFirst();
                        break;
                    case CommandMode.PasteAsPascalCase:
                        result = string.Join("", words.Select(x => x.ToLower().ToUpperFirst()));
                        break;
                    case CommandMode.PasteAsSnakeCase:
                        result = string.Join("_", words.Select(x => x.ToLower()));
                        break;
                }

                var selection = textDocument.Selection;
                if (selection != null)
                {
                    selection.Insert(result);
                }
            }
        }       
    }
}
