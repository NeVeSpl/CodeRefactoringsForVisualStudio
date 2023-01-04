using InvertAssignmentDirection;
using Microsoft.CodeAnalysis.CodeRefactorings;
using NUnit.Framework;
using RenameVariableAfterType;

namespace CodeRefactoringsForVisualStudio.Tests.RenameVariable
{
    public class RenameVariableTests : BaseCodeRefactoringTestFixture
    {
        protected override CodeRefactoringProvider CreateProvider()
        {
            return new RenameVariableAfterTypeCodeRefactoringProvider();
        }


        [TestCase("PassBasicSmokeTest")]
      
        public void Should(string caseName)
        {
            TestCodeRefactoring("RenameVariable.Data", caseName);
        }
    }
}