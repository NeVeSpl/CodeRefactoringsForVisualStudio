using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Test.InvertAssignmentDirection.Data
{
    class C
    {
        void M()
        {
            int i = 1;
            int j = 2;
            [|i = j;
            j = i;|]
        }
    }
}
