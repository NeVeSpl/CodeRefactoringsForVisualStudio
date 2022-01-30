using System;
using System.Collections.Generic;
using System.Text;
using GenerateMapping;
using Microsoft.CodeAnalysis.CodeRefactorings;
using NUnit.Framework;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping
{
    internal class GenerateMappingTests : BaseCodeRefactoringTestFixture
    {
        protected override CodeRefactoringProvider CreateProvider()
        {
            return new GenerateMappingCodeRefactoringProvider();
        }

        [TestCase("ArrayType")]
        [TestCase("CollectionType")]
        [TestCase("ReferenceType")]
        [TestCase("StringType")]
        [TestCase("ValueType")]
        public void ShouldAssign(string caseName)
        {
            TestCodeRefactoring("GenerateMapping.Assignment", caseName);
        }

        [TestCase("Flattening")]
        [TestCase("FromBase")]
        [TestCase("FromSimpleParams")]
        [TestCase("FromMethod")]
        [TestCase("ToFields")]
        [TestCase("ToFieldsWhenPropIsReadOnly")]
        [TestCase("ToProp")]
        public void ShouldMatch(string caseName)
        {
            TestCodeRefactoring("GenerateMapping.Matching", caseName);
        }

        [TestCase("CopyConstructor")]
        [TestCase("MethodReturnsClassNoParam")]
        [TestCase("MethodReturnsClassOneParam")]
        [TestCase("MethodReturnsToupleOneParam")]
        [TestCase("MethodReturnsVoidOneParam")]
        [TestCase("ObjectCreationExpression")]
        public void ShouldMapInside(string caseName)
        {
            TestCodeRefactoring("GenerateMapping.Context", caseName);
        }

        [TestCase("Generic")]
        public void ShouldMapSpecial(string caseName)
        {
            TestCodeRefactoring("GenerateMapping.Special", caseName);
        }
    }
}
