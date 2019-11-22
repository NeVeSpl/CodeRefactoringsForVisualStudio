using InvertAssignmentDirection;
using Microsoft.CodeAnalysis.CodeRefactorings;
using NUnit.Framework;

namespace CodeRefactoringsForVisualStudio.Tests.InvertAssignmentDirectionTests
{
    public class InvertAssignmentDirectionTests : BaseCodeRefactoringTestFixture
    {
        protected override CodeRefactoringProvider CreateProvider()
        {
            return new InvertAssignmentDirectionRefactoringProvider();
        }


        [TestCase("ShouldInvertSelectedSingleAssignment")]
        [TestCase("ShouldInvertSelectedTwoAssignments")]
        [TestCase("ShouldPreserveLeftTrivia")]
        [TestCase("ShouldPreserveRightTrivia")]
        [TestCase("ShouldInvertAssignmentWithMemberAccessOnBothSides")]
        [TestCase("ShouldInvertAssignmentWithMemberAccessOnOneSide")]
        public void Should(string caseName)
        {
            TestCodeRefactoring("InvertAssignmentDirection.Data", caseName);
        }
    }
}