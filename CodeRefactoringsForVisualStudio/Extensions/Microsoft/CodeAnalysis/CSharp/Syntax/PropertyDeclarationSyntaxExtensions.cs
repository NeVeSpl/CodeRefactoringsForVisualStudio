using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public static class PropertyDeclarationSyntaxExtensions
    {
        public static bool IsAutoProperty(this PropertyDeclarationSyntax property)
        {
            return property.AccessorList.Accessors.All(x => x.Body == null);
        }
    }
}
