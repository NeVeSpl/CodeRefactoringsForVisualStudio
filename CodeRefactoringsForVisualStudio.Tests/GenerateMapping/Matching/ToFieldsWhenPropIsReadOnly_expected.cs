using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        private int a;
        private string _B;


        public int A
        {
            get { return a; }
        }
        public double B
        {
            get => _B;
        }


        void Fuu(Foo other)
        {
            this.a = other.A;
            this._B = other.B;
        }
    }
}