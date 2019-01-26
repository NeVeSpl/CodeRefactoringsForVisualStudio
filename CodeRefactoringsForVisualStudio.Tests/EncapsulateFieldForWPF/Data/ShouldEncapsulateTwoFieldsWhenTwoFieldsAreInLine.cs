using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Test.EncapsulateFieldsForWPF.Data
{
    class ShouldEncapsulateOneField
    {
        [|int mFoo, _Bar|];
    }
}