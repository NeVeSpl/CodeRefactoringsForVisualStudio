using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Test.EncapsulateFieldsForWPF.Data
{
    class ShouldEncapsulateOneField
    {
        int mFoo, _Bar;
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
        public int Bar
        {
            get
            {
                return _Bar;
            }
            set
            {
                _Bar = value;
                OnPropertyChanged();
            }
        }
    }
}