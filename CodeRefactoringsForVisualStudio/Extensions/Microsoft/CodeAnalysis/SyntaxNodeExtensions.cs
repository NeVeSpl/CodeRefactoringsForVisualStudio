using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public static class SyntaxNodeExtensions
    {
        public static IEnumerable<T> ExtractSelectedNodesOfType<T>(this SyntaxNode rootNode, TextSpan selection) where T : SyntaxNode
        {
            SyntaxNode currentNode = rootNode.FindNode(selection);
            IEnumerable<T> result = currentNode.DescendantNodes(selection).OfType<T>();

            if (!result.Any())
            {
                do
                {
                    if (currentNode is T singleResult)
                    {
                        result = new[] { singleResult };
                        break;
                    }
                    currentNode = currentNode.Parent;

                } while (currentNode != null);
            }

            return result;
        }
    }
}
