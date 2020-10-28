using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    public static class SyntaxFactoryEx
    {
        public static NamespaceDeclarationSyntax NamespaceDeclaration(string name)
        {
            return SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(name)).NormalizeWhitespace();
        }

        public static ClassDeclarationSyntax InternalClassDeclaration(string identifier)
        {
            return SyntaxFactory.ClassDeclaration(identifier).AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
        }
        public static ClassDeclarationSyntax PublicClassDeclaration(string identifier)
        {
            return SyntaxFactory.ClassDeclaration(identifier).AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        }

        public static ParameterSyntax Parameter(string identifier, string type)
        {  
            return SyntaxFactory.Parameter(SyntaxFactory.Identifier(identifier)).WithType(SyntaxFactory.IdentifierName(type));
        }
    }
}
