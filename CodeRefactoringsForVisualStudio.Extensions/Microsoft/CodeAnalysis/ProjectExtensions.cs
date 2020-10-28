using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis
{
    public static class ProjectExtensions
    {
       
        public static Document AddDocument(this Project project, DocumentTemplate fileTemplate)
        {
            return project.AddDocument(fileTemplate.FileName, fileTemplate.Syntax, fileTemplate.SolutionFolders);
        }
    }



    public sealed class DocumentTemplate
    {
        public string FileName { get; set; }
        public CompilationUnitSyntax Syntax { get; set; }
        public IEnumerable<string> SolutionFolders { get; set; }
    }
}
