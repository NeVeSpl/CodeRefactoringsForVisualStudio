using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace CopyPasteWithConversion.Vsix.Internals
{
    internal static class VSHelper
    {
        public static async Task<IWpfTextView> GetActiveIWpfTextView(AsyncPackage package)
        {
            var vsTextManager = await package.GetServiceAsync<SVsTextManager, IVsTextManager>();
            var getActiveViewErrorCode = vsTextManager.GetActiveView(1, null, out var textView);

            var mefServiceProvider = await package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var vsEditorAdaptersFactoryService = mefServiceProvider.GetService<IVsEditorAdaptersFactoryService>();

            var wpfTextView = vsEditorAdaptersFactoryService?.GetWpfTextView(textView);
            return wpfTextView;
        }
    }
}
