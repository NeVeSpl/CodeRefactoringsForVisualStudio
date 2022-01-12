using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CopyPasteWithConversion
{
    internal sealed class CopyCommand
    {       
        public enum CommandMode 
        {
            CopyAsSeparateWords = 0x0100,
            CopyAsCamelCase = 0x0110,
            CopyAsPascalCase = 0x120,
            CopyAsSnakeCase = 0x0130,
            CopyAsSentenceCase = 0x0140
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
        private CopyCommand(AsyncPackage package, OleMenuCommandService commandService, CommandMode mode)
        {
            this.mode = mode;
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            CommandId = (int)mode;
            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);       
            commandService.AddCommand(menuItem);
        }

      
        public static async Task InitializeAsync(AsyncPackage package, CommandMode mode)
        {            
            // Switch to the main thread - the call to AddCommand in CopyAsSeparateWords's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);         
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            new CopyCommand(package, commandService, mode);
        }

        private async void ExecuteAsync()
        {
            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView == null) return; //not a text window     


            var selection = docView?.TextView.Selection;
            if (selection.IsEmpty == false)
            {
                var selectedText = selection.StreamSelectionSpan.GetText();
                var words = selectedText.SplitStringIntoSeparateWords();
                var result = String.Empty;
                switch (mode)
                {
                    case CommandMode.CopyAsSeparateWords:
                        result = string.Join(" ", words);
                        break;
                    case CommandMode.CopyAsCamelCase:
                        result = string.Join("", words.Select(x => x.ToLower().ToUpperFirst())).ToLowerFirst();
                        break;
                    case CommandMode.CopyAsPascalCase:
                        result = string.Join("", words.Select(x => x.ToLower().ToUpperFirst()));
                        break;
                    case CommandMode.CopyAsSnakeCase:
                        result = string.Join("_", words.Select(x => x.ToLower()));
                        break;
                    case CommandMode.CopyAsSentenceCase:
                        result = string.Join(" ", words.Select(x => x.ToLower())).ToUpperFirst();
                        break;
                }

                try
                {
                    Clipboard.SetDataObject(result, true, 4, 250);
                }
                catch (ExternalException)
                {

                }
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            ExecuteAsync();
        }       
    }
}