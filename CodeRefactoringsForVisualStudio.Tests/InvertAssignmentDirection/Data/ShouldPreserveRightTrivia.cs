using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Test.InvertAssignmentDirection.Data
{
    class ShouldPreserveLeftTrivia
    {
        public void Foo(int a, int b)
        {
            [|a = b;|]  // dfds fsd
        }
    }
}
