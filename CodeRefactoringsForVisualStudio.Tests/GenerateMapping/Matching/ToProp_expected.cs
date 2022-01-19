using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        private int a;
        private double _b;

        public int A
        {
            get { return a; }
            set { a = value; }
        }
        public double B
        {
            get => _b;
            set => _b = value;
        }
        public string C { get; private set; }
        public string D { get; init set; }


        void Fuu(Foo other)
        {
            this.A = other.A;
            this.B = other.B;
            this.C = other.C;
            this.D = other.D;
        }
    }
}