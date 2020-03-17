using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Test.InvertAssignmentDirection.Data
{
    class Foo
    {
        void Fuu(Foo foo1, Foo foo2)
        {
            int[] arry = new int [] { 1, 2, 3 };
            int[] bary = new int [] { 4, 5, 6 };

            bary[2] = arry[1];
        }
    }
}
