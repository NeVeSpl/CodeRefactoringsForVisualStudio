using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Test.EncapsulateFieldsForWPF.Data
{
    class ShouldEncapsulateOneField
    {
        int mFoo;
        public int Foo
        {
            get
            {
                return mFoo;
            }

            set
            {
                mFoo = value;
                OnPropertyChanged();
            }
        }
    }
}
