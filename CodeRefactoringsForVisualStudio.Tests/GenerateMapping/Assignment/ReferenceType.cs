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

        [|void Fuu1(Foo2 source)|]
        {            
             
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