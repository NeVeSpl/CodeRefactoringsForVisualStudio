using System;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public static class TypeSyntaxExtensions
    {
        public static TypeSyntax UnpackTypeFromTaskAndActionResult(this TypeSyntax type)
        {
            var toRemove = new[] { "Task", "ActionResult" };

            for (int i = 0; i < 2; ++i)
            {
                if ((type is GenericNameSyntax generic) && (toRemove.Contains(generic.Identifier.ValueText)) && generic.TypeArgumentList.Arguments.Count == 1)
                {
                    type = generic.TypeArgumentList.Arguments.First();
                }
            }

            return type;
        }
    }
}
