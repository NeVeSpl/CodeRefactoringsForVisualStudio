﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using System.Threading;
using GenerateMapping.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping
{
    internal class GenerateMapping_AccesorTests
    {
        [Test]
        public void AccessorsExtractor_GetAccessors()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
        """
        using System;
        
        public class MyType : BaseClass
        {
            public static readonly int field_static_readonly;
            public readonly int field_readonly;
            public const int field_const = 7;
            public int field;
            
            public int Prop { get; set; }
            public int Prop_readonly { get;  }
            public int Prop_init { get; init; }
            public int Prop_private { get; private set; }
            public int Prop_expresion => 7;

            public Other other = new Other();


            public MyType()
            {
            }
        }
        public class BaseClass
        {
            public int Base_Prop { get; set; }
            protected int Base_Prop_protected { get; protected set; }
            private int Base_Prop_private { get; set; }
            public int Base_Prop_private_set { get; private set; }
        }
        public class Other
        {
            public int Other_Prop { get; set; }
        }

        """
    );

            var root = tree.GetRoot();
            var compilation = CSharpCompilation.Create("HelloWorld")
                                               .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                                               .AddSyntaxTrees(tree);
            var semanticModel = compilation.GetSemanticModel(tree);

            var node = root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>().First();

            (var leftAccessors, var rightAccessors) = AccessorsExtractor.GetAccessors(semanticModel, node, default);

           
        }
    }
}
