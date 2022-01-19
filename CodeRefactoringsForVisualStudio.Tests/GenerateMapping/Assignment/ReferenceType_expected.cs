using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo1
    {
        public AClass A { get; set; }
        public BClass B { get; set; }
        public CClass<int> C { get; set; }

        void Fuu1(Foo2 source)
        {
            this.A = new AClass(source.A);
            this.B = new BClass(source.B);
            this.C = new CClass<int>(source.C);
        }
    }

    class Foo2
    {
        public AClass A { get; set; }
        public AClass B { get; set; }
        public CClass<int> C { get; set; }
    }



    class AClass
    {

    }
    class BClass
    {

    }
    class CClass<T>
    {

    }
}