using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        public int A
        {
            get;
            set;
        }
        public double B
        {
            get;
            set;
        }


        void Fuu(Foo2 other)
        {
            this.A = other.GetA();
        }
    }


    class Foo2
    {
        public int GetA()
        {
            return 7
        }

        private double GetB()
        {
            return 0.0;
        }
    }
}