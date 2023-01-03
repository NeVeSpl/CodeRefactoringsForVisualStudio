using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using RoslynTestKit;

namespace CodeRefactoringsForVisualStudio.Tests
{
    public abstract class BaseCodeRefactoringTestFixture : CodeRefactoringTestFixture
    {
        protected override string LanguageName { get => Microsoft.CodeAnalysis.LanguageNames.CSharp; }
        protected override bool FailWhenInputContainsErrors => false;


        protected void TestCodeRefactoring(string folderWithData, string caseName)
        {
            string inputMarkupCode = LoadResource(folderWithData, caseName);
            string expectedMarkupCode = LoadResource(folderWithData, caseName + "_expected");

            base.TestCodeRefactoring(inputMarkupCode, expectedMarkupCode);
        }

        private string LoadResource(string folderWithData, string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"CodeRefactoringsForVisualStudio.Tests.{folderWithData}.{fileName}.cs";

            string result;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
            }

            return result;
        }
    }
}
