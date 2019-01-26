using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Test.InvertAssignmentDirection.Data
{
    class ShouldPreserveLeftTrivia
    {
        public int Foo(int a, int b)
        {
            b = a;  // dfds fsd
        }
    }
}
