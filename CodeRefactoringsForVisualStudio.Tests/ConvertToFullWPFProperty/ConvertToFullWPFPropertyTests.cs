using System.IO;
using System.Reflection;
using CodeRefactoringsForVisualStudio.Refactorings.ConvertToFullWPFProperty;
using CodeRefactoringsForVisualStudio.Tests;
using Microsoft.CodeAnalysis.CodeRefactorings;
using NUnit.Framework;
using RoslynNUnitLight;

namespace CodeRefactoringsForVisualStudio.Tests.ConvertToFullWPFProperty
{
    public class ConvertToFullWPFPropertyTests : BaseCodeRefactoringTestFixture
    {
        protected override CodeRefactoringProvider CreateProvider()
        {
            return new ConvertToFullWPFPropertyRefactoringProvider();
        }



        
        //[TestCase("Class1")]
        //public void Should(string caseName)
        //{
        //    TestCodeRefactoring("ConvertToFullWPFProperty.Data", caseName);
        //}
    }
}
